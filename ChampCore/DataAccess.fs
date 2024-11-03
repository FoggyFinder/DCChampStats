module Champs.Db

[<RequireQualifiedAccess>]
module internal SQL =
    let createTablesSQL = """
        CREATE TABLE IF NOT EXISTS Champ (
	        ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            AssetID INTEGER NOT NULL UNIQUE,
            Name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Battle (
            ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            BattleNum INTEGER NOT NULL UNIQUE,
            WinnerID INTEGER NOT NULL,
            LoserID INTEGER NOT NULL,
            Description TEXT NOT NULL,
            Wager INTEGER NOT NULL,
            FOREIGN KEY (WinnerID)
               REFERENCES Champ (ID),
            FOREIGN KEY (LoserID)
               REFERENCES Champ (ID)
        );

        CREATE TABLE IF NOT EXISTS KeyValue (
            Key TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL
        );
    """

    let GetLastTrackedBattle = 
        "SELECT Value FROM KeyValue WHERE Key = 'LastTrackedBattle'"
    
    let SetLastTrackedBattle = "
        INSERT INTO KeyValue(Key, Value) VALUES('LastTrackedBattle', @lastTrackedBattle)
        ON CONFLICT(Key) DO UPDATE SET Value = @lastTrackedBattle;"

    let ChampExists = 
        "SELECT EXISTS(SELECT 1 FROM Champ WHERE AssetID = @assetId LIMIT 1);"

    let BattleExists =
        "SELECT EXISTS(SELECT 1 FROM Battle WHERE BattleNum = @battleNum LIMIT 1);"

    let AddOrUpdateChamp = "
        INSERT INTO Champ(AssetID, Name) VALUES(@assetId, @name)
        ON CONFLICT(AssetID) DO UPDATE SET Name = @name;
 "

    let AddOrUpdateBattle = "
        INSERT INTO Battle(BattleNum, WinnerID, LoserID, Description, Wager)
        VALUES(@battleNum, @winnerId, @loserId, @description, @wager)
        ON CONFLICT(BattleNum) DO
        UPDATE SET WinnerID = @winnerId, LoserID = @loserId, Description = @description, Wager = @wager;
 "
    let GetChampIdByAssetId = "
        SELECT ID FROM Champ
        WHERE AssetID = @assetId
    "

    let GetChampByAssetId = "
        SELECT Name, AssetID FROM Champ
        WHERE AssetID = @assetId
    "

    let GetBattleByBattleNum = "
        SELECT BattleNum, Description, Wager, wc.AssetID as WAssetId, wc.Name as Winner, lc.AssetID as LAssetId, lc.Name as Loser
        FROM Battle
        JOIN Champ wc ON wc.ID = Battle.WinnerID
        JOIN Champ lc ON lc.ID = Battle.LoserID
        WHERE BattleNum = @battleNum
    "

    let GetAllChamps = "
        SELECT Name, AssetID FROM Champ
    "

    let GetAllBattles = "
        SELECT BattleNum, Description, Wager, wc.AssetID as WAssetId, wc.Name as Winner, lc.AssetID as LAssetId, lc.Name as Loser
        FROM Battle
        JOIN Champ wc ON wc.ID = Battle.WinnerID
        JOIN Champ lc ON lc.ID = Battle.LoserID
    "

open Champs.Core
open System.Collections.Generic
open Microsoft.Data.Sqlite
open Donald

type SqliteStorage(cs: string)=
    let conn = new SqliteConnection(cs)
    do Db.newCommand SQL.createTablesSQL conn
       |> Db.exec
    do conn.Dispose()

    let getChampIdByAssetId(assetId: uint64) =
        try 
            use conn = new SqliteConnection(cs)
            Db.newCommand SQL.GetChampIdByAssetId conn
            |> Db.setParams [ "assetId", SqlType.Int64 <| int64 assetId ]
            |> Db.scalar (fun v -> tryUnbox<int64> v)
        with _ -> None

    member t.GetLastTrackedBattle() =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetLastTrackedBattle conn
        |> Db.query (fun rd -> rd.ReadString "Value")
        |> List.tryHead

    member t.SetLastTrackedBattle(battle:uint64) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.SetLastTrackedBattle conn
        |> Db.setParams [ "lastTrackedBattle", SqlType.Int64 <| int64 battle ]
        |> Db.exec

    member t.ChampExists(assetId: uint64) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.ChampExists conn
        |> Db.setParams [ "assetId", SqlType.Int64 <| int64 assetId ]
        |> Db.scalar (fun v -> tryUnbox<int64> v |> Option.map(fun v -> v > 0) |> Option.defaultValue false)
        
    member t.BattleExists(battleNum: uint64) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.BattleExists conn
        |> Db.setParams [ "battleNum", SqlType.Int64 <| int64 battleNum ]
        |> Db.scalar (fun v -> tryUnbox<int64> v |> Option.map(fun v -> v > 0) |> Option.defaultValue false)
    
    member t.AddOrInsertChamp(champ:Champ) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.AddOrUpdateChamp conn
        |> Db.setParams [
            "assetId", SqlType.Int64 <| int64 champ.AssetId
            "name", SqlType.String champ.Name
        ]
        |> Db.exec
        
    member t.AddOrUpdateBattle(battle:Battle) =
        use conn = new SqliteConnection(cs)
        match getChampIdByAssetId battle.Winner.AssetId, getChampIdByAssetId battle.Loser.AssetId with
        | Some winnerId, Some loserId ->
            Db.newCommand SQL.AddOrUpdateBattle conn
            |> Db.setParams [
                "battleNum", SqlType.Int64 <| int64 battle.BattleNum
                "winnerId", SqlType.Int64 <| winnerId
                "loserId", SqlType.Int64 <| loserId
                "description", SqlType.String <| battle.Description
                "wager", SqlType.Decimal <| battle.Wager
            ]
            |> Db.exec
        | _ -> ()

    member t.TryGetChamp(assetId: uint64) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetChampByAssetId conn
        |> Db.setParams [
            "assetId", SqlType.Int64 <| int64 assetId
        ]
        |> Db.querySingle(fun reader -> 
            {
                Name = reader.GetString(0);
                AssetId = uint64 (reader.GetInt64(1))
            })

    member t.TryGetBattle(battleNum: uint64) : Battle option =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetBattleByBattleNum conn
        |> Db.setParams [
            "battleNum", SqlType.Int64 <| int64 battleNum
        ]
        |> Db.querySingle(fun reader -> 
            {
                BattleNum = uint64 (reader.GetInt64(0))
                Description = reader.GetString(1)
                Wager = reader.GetDecimal(2)
                Winner = {
                    AssetId = reader.GetInt64(3) |> uint64
                    Name = reader.GetString(4)
                }
                Loser = {
                    AssetId = reader.GetInt64(5) |> uint64
                    Name = reader.GetString(6)
                }
            })

    member t.GetAllChamps() =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetAllChamps conn
        |> Db.query(fun reader -> 
            {
                Name = reader.GetString(0);
                AssetId = uint64 (reader.GetInt64(1))
            })

    member t.GetAllBattles() : Battle list =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetAllBattles conn
        |> Db.query(fun reader -> 
            {
                BattleNum = uint64 (reader.GetInt64(0))
                Description = reader.GetString(1)
                Wager = reader.GetDecimal(2)
                Winner = {
                    AssetId = reader.GetInt64(3) |> uint64
                    Name = reader.GetString(4)
                }
                Loser = {
                    AssetId = reader.GetInt64(5) |> uint64
                    Name = reader.GetString(6)
                }
            })

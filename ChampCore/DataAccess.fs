module Champs.Db

type DbKeys =
    | LastTrackedBattle
    | LastTrackedTraitSwap
    | LastTrackedBattleDateTime

[<RequireQualifiedAccess>]
module internal SQL =
    let createTablesSQL = """
        CREATE TABLE IF NOT EXISTS Champ (
	        ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            AssetID INTEGER NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            IPFS TEXT
        );

        CREATE TABLE IF NOT EXISTS Battle (
            ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            BattleNum INTEGER NOT NULL UNIQUE,
            WinnerID INTEGER NOT NULL,
            LoserID INTEGER NOT NULL,
            Description TEXT NOT NULL,
            Wager INTEGER NOT NULL,
            Timestamp DATETIME,
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

    let AlterBattleTable = """
        ALTER TABLE Battle
        ADD COLUMN Timestamp DATETIME
    """

    let getTimestampColumnInfo = """
        select count(*) from
        pragma_table_info('Battle')
        where name='Timestamp'

    """

    let GetValueByKey = 
        "SELECT Value FROM KeyValue WHERE Key = @key"
    
    let SetKeyValue = "
        INSERT INTO KeyValue(Key, Value) VALUES(@key, @value)
        ON CONFLICT(Key) DO UPDATE SET Value = @value;"

    let ChampExists = 
        "SELECT EXISTS(SELECT 1 FROM Champ WHERE AssetID = @assetId LIMIT 1);"

    let BattleExists =
        "SELECT EXISTS(SELECT 1 FROM Battle WHERE BattleNum = @battleNum LIMIT 1);"

    let AddOrUpdateChamp = "
        INSERT INTO Champ(AssetID, Name, IPFS) VALUES(@assetId, @name, @ipfs)
        ON CONFLICT(AssetID) DO UPDATE SET Name = @name, IPFS = @ipfs;"

    let AddOrUpdateBattle = "
        INSERT INTO Battle(BattleNum, WinnerID, LoserID, Description, Wager, Timestamp)
        VALUES(@battleNum, @winnerId, @loserId, @description, @wager, @timestamp)
        ON CONFLICT(BattleNum) DO
        UPDATE SET WinnerID = @winnerId, LoserID = @loserId, Description = @description, Wager = @wager, Timestamp = @timestamp;
 "
    let GetChampIdByAssetId = "
        SELECT ID FROM Champ
        WHERE AssetID = @assetId
    "

    let GetChampByAssetId = "
        SELECT Name, AssetID, IPFS FROM Champ
        WHERE AssetID = @assetId
    "

    let GetBattleByBattleNum = "
        SELECT BattleNum, Description, Wager, 
            wc.AssetID as WAssetId, wc.Name as Winner, wc.IPFS as WIPFS,
            lc.AssetID as LAssetId, lc.Name as Loser, lc.IPFS as LIPFS,
            Timestamp
        FROM Battle
        JOIN Champ wc ON wc.ID = Battle.WinnerID
        JOIN Champ lc ON lc.ID = Battle.LoserID
        WHERE BattleNum = @battleNum
    "

    let GetAllChamps = "
        SELECT Name, AssetID, IPFS FROM Champ
    "

    let GetBattlesWithoutTimestamp = "
        SELECT BattleNum FROM Battle WHERE Timestamp IS NULL
    "

    let GetAllBattles = "
        SELECT
            BattleNum, Description, Wager,
            wc.AssetID as WAssetId, wc.Name as Winner, wc.IPFS as WIPFS,
            lc.AssetID as LAssetId, lc.Name as Loser, lc.IPFS as LIPFS,
            Timestamp
        FROM Battle
        JOIN Champ wc ON wc.ID = Battle.WinnerID
        JOIN Champ lc ON lc.ID = Battle.LoserID
    "

open Champs.Core
open System.Collections.Generic
open Microsoft.Data.Sqlite
open Donald
open System

type SqliteStorage(cs: string)=
    let conn = new SqliteConnection(cs)
    do Db.newCommand SQL.createTablesSQL conn
       |> Db.exec
    do Db.newCommand SQL.getTimestampColumnInfo conn
       |> Db.scalar(fun v -> tryUnbox<int64> v)
       |> Option.iter(fun i ->
         if i = 0L then
            Db.newCommand SQL.AlterBattleTable conn
            |> Db.exec)
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
        Db.newCommand SQL.GetValueByKey conn
        |> Db.setParams [ "key", SqlType.String (DbKeys.LastTrackedBattle.ToString()) ]
        |> Db.query (fun rd -> rd.ReadString "Value")
        |> List.tryHead

    member _.GetLastTrackedBattleDateTime() =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetValueByKey conn
        |> Db.setParams [ "key", SqlType.String (DbKeys.LastTrackedBattleDateTime.ToString()) ]
        |> Db.query (fun rd -> rd.ReadDateTime "Value")
        |> List.tryHead
        
    member t.SetLastTrackedBattleDateTime(dt:DateTime) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.SetKeyValue conn
        |> Db.setParams [
            "key", SqlType.String (DbKeys.LastTrackedBattleDateTime.ToString())
            "value", SqlType.DateTime dt
        ]
        |> Db.exec    
    
    member t.SetLastTrackedBattle(battle:uint64) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.SetKeyValue conn
        |> Db.setParams [
            "key", SqlType.String (DbKeys.LastTrackedBattle.ToString())
            "value", SqlType.Int64 <| int64 battle 
        ]
        |> Db.exec

    member t.GetLastTrackedTraitSwap() =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetValueByKey conn
        |> Db.setParams [ "key", SqlType.String (DbKeys.LastTrackedTraitSwap.ToString()) ]
        |> Db.query (fun rd -> rd.ReadString "Value")
        |> List.tryHead

    member t.SetLastTrackedTraitSwap(round:uint64) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.SetKeyValue conn
        |> Db.setParams [
            "key", SqlType.String (DbKeys.LastTrackedTraitSwap.ToString())
            "value", SqlType.Int64 <| int64 round
        ]
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
    
    member t.AddOrUpdateChamp(champ:Champ) =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.AddOrUpdateChamp conn
        |> Db.setParams [
            "assetId", SqlType.Int64 <| int64 champ.AssetId
            "name", SqlType.String champ.Name
            "ipfs", if champ.Ipfs.IsSome then SqlType.String champ.Ipfs.Value else SqlType.Null
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
                "timestamp", if battle.UTCDateTime.IsSome then SqlType.DateTime battle.UTCDateTime.Value else SqlType.Null
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
                AssetId = uint64 (reader.GetInt64(1));
                Ipfs = if reader.IsDBNull(2) then None else Some(reader.GetString(2))
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
                    Ipfs = if reader.IsDBNull(5) then None else Some(reader.GetString(5))
                }
                Loser = {
                    AssetId = reader.GetInt64(6) |> uint64
                    Name = reader.GetString(7)
                    Ipfs = if reader.IsDBNull(8) then None else Some(reader.GetString(8))
                }
                UTCDateTime = if reader.IsDBNull(9) then None else Some(reader.GetDateTime(9))
            })

    member t.GetAllChamps() =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetAllChamps conn
        |> Db.query(fun reader -> 
            {
                Name = reader.GetString(0);
                AssetId = uint64 (reader.GetInt64(1))
                Ipfs = if reader.IsDBNull(2) then None else Some(reader.GetString(2))
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
                    Ipfs = if reader.IsDBNull(5) then None else Some(reader.GetString(5))
                }
                Loser = {
                    AssetId = reader.GetInt64(6) |> uint64
                    Name = reader.GetString(7)
                    Ipfs = if reader.IsDBNull(8) then None else Some(reader.GetString(8))
                }
                UTCDateTime = if reader.IsDBNull(9) then None else Some(reader.GetDateTime(9))
            })
    
    member t.BattlesWithoutTimestamp() : uint64 list =
        use conn = new SqliteConnection(cs)
        Db.newCommand SQL.GetBattlesWithoutTimestamp conn
        |> Db.query(fun reader -> uint64 <| reader.GetInt64(0))
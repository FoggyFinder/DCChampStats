module Champs.Pages.FAQ

open Falco.Markup
open Champs.Core

let faqPage() = [
    yield Text.h1 $"FAQ"
    yield Elem.div [ Attr.class' "QA" ] [
        Elem.div [ Attr.class' "Q" ] [
            Text.h2 "What is DarkCoin ?"
        ]

        Elem.div [ Attr.class' "A" ] [
            Elem.p [ ] [
                Elem.a [ Attr.href $"https://dark-coin.io/" ] [ Text.raw $"Dark Coin" ]
                Text.raw " is an experimental community project based on the Algorand blockchain, aiming to expand the decentralized finance (DeFi) ecosystem. Managed by a decentralized autonomous organization (DAO), Dark Coin lets users participate actively in project governance."
            ]

            Text.p "Key Features: "

            Text.p " — Decentralized Governance: Users can engage in shaping the project's future through the Dark Coin dApp by voting on proposals and decisions. "
            Text.p " — Champion NFT Assets and AI Arena: Users can own unique character NFT assets and battle in the Dark Coin AI Arena for an interactive experience. "
        ]
    ]

    yield Elem.div [ Attr.class' "QA" ] [
        yield Elem.div [ Attr.class' "Q" ] [
            Text.h2 "What is DarkCoin Arena ?"
        ]

        yield Elem.div [ Attr.class' "A" ] [
            Elem.p [ ] [
                Elem.a [ Attr.href $"https://dark-coin.com/arena" ] [ Text.raw $"Dark Coin" ]
                Text.raw " is an engaging and interactive application within the Dark Coin ecosystem where users can battle with their Dark Coin champion NFTs for rewards and glory"
            ]

            Text.p "Here is a detailed overview of the Dark Coin Arena:"

            Elem.ol [] [
                Elem.li [] [
                    Text.h4 "Champion NFTs and Trait Swapping"
                
                    Text.p " — Dark Coin champion NFTs are built on Algorand's ARC-19 standard, allowing for trait swapping."
                    Text.p " — Users can visit the trait swapper section inside the Arena to customize their champion's appearance by mixing and matching traits."
                    Text.p " — Equipped traits are sent to a contract for holding, while unequipped traits are stored in the user's wallet."
                ]

                Elem.li [] [
                    Text.h4 "Selecting a Champion for Battle"

                    Text.p " — In the Arena, users can select the champion they wish to use in a battle from their collection."
                ]

                Elem.li [] [
                    Text.h4 "Initializing or Joining a Battle"

                    Text.p " — To initiate a battle, users can start a new battle within the Arena."
                    Text.p " — Users can also join an existing battle by paying a 10,000 Dark Coin wager per participant and an additional 0.1 Algo fee."
                ]

                Elem.li [] [
                    Text.h4 "Battle Process"

                    Text.p " — When a champion joins a battle, the Arena contract determines the winner based on certain criteria."
                    Text.p " — The victorious champion receives the combined 20,000 Dark Coin wager from both participants."
                    Text.p " — The Arena application uses AI to generate a battle story describing the victory and creates an image illustrating the battle outcome."
                    Text.p " — Battle results, including the story and images, are shared in a dedicated Discord channel for community viewing."
                ]
            ]
        ]
    ]
    
    yield Elem.div [ Attr.class' "QA" ] [
        Elem.div [ Attr.class' "Q" ] [
            Text.h2 "Why this site was created ?"
        ]

        Elem.div [ Attr.class' "A" ] [
            Text.p "Few reasons:"

            Text.p "1. Leaderboard from the main dark-coin app wasn't loaded for quite some time so quick tool to see Champ's ranking would be useful."
            Text.p "2. Every 100-200 rounds top-3 Champs and 1 most unfortunate Champ receive NFT so knowing current ranking could help select right strategy."
            Text.p "3. To learn web-dev ;)"
        ]
    ]
]
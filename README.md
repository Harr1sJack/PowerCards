Multiplayer Card 1v1 Game

A turn-based multiplayer card strategy game built with Unity and Netcode for GameObjects. Players compete over 6 turns, managing energy costs and utilizing card abilities to outscore their opponent.

Technical Implementation

1. Networking Solution

This project uses Unity Netcode for GameObjects (NGO) with the default Unity Transport (UTP).

Topology: Host-Server architecture.

Custom Messaging: Instead of creating networked variables for every piece of data, the game uses a custom NetworkAdapter class.

RPCs: Game events are serialized into a single JSONEvent class and transmitted via SendJsonToServerRpc and BroadcastJsonClientRpc.


2. JSON Card System

Cards are defined as ScriptableObjects (CardSO) in the editor for easy design, but logic is handled via data:

Definitions: Each card has an ID, Cost, Power, and an Ability Enum (e.g., StealPoints, DrawExtraCard).

Color Schema : 
    Ability value and type is marked in yellow color

    Power value is marked in green color

    Cost value is marked in the red color

State Sync: When a turn starts or ends, the server sends a JSON payload containing only integers (Card IDs, Scores, Deck Counts). The clients use these IDs to look up visual data (Sprites, Descriptions) from their local Resources.

How to Run & Test:

Connection : Hotspot or LAN

Launch the Mobile APK in 2 Mobile Phones

Host needs to Create Room while the client can join using Host IP

Play:

Both players draw cards automatically.

Select cards within your cost budget and click "Play Turn".

Wait for the opponent to submit.

Watch the simultaneous reveal and score update!

Ensure the mobile devices are on the same Wi-Fi network.
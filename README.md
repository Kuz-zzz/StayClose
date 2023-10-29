# StayClose
Config options (tshock/stayclose.json):
- killEveryone: if true, all players die if one goes too far;
- enableTeams: the player dies if they are too far from their team even if other players are nearby;
- secondsUntilDeath: time players have to go back to others until they die. If 0, players die instantly;
- distance: distance in blocks that is considered "far".

Usage:
- put StayClose.dll in ServerPlugins

Note that lag can do funny things and make the game nearly impossible. 
It checks players' positions every 2 seconds and when players move (falling does not count), so keep in mind that sometimes you'll have to move a little for it to recognize your position (blame relogic).

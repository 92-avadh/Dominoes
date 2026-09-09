using System;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Supported game modes in Dominoes MVP.
    /// </summary>
    public enum GameModeType
    {
        VsComputer,
        OnlineMatchmaking,
        FriendRoom
    }

    /// <summary>
    /// AI difficulty levels for offline VS Computer matches.
    /// </summary>
    public enum ComputerDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Global authoritative context holding the selected game mode, rules, difficulty,
    /// and room state across screen transitions (HomeScreen -> WaitingScreen -> GameScreen).
    /// </summary>
    public static class DominoGameModeContext
    {
        public static GameModeType CurrentMode { get; set; } = GameModeType.VsComputer;
        public static ComputerDifficulty Difficulty { get; set; } = ComputerDifficulty.Medium;
        public static string OnlineRule { get; set; } = "Classic";
        public static string RoomCode { get; set; } = "DOM-1001";
        public static bool IsHost { get; set; } = true;

        /// <summary>
        /// Gets a formatted display badge for in-game top bar HUD.
        /// </summary>
        public static string GetGameScreenHeader()
        {
            switch (CurrentMode)
            {
                case GameModeType.VsComputer:
                    return $"VS AI • {Difficulty.ToString().ToUpper()}";
                case GameModeType.OnlineMatchmaking:
                    return $"ONLINE • {OnlineRule.ToUpper()}";
                case GameModeType.FriendRoom:
                    return $"FRIEND • {RoomCode}";
                default:
                    return "DOMINOES";
            }
        }

        /// <summary>
        /// Gets the main header title for the waiting lobby.
        /// </summary>
        public static string GetLobbyTitle()
        {
            switch (CurrentMode)
            {
                case GameModeType.VsComputer:
                    return $"SOLO MATCH ({Difficulty.ToString().ToUpper()})";
                case GameModeType.OnlineMatchmaking:
                    return "FINDING PLAYERS";
                case GameModeType.FriendRoom:
                    return $"PRIVATE ROOM: {RoomCode}";
                default:
                    return "DOMINOES LOBBY";
            }
        }

        /// <summary>
        /// Gets the subtitle/mode indicator for the waiting lobby.
        /// </summary>
        public static string GetLobbySubtitle()
        {
            switch (CurrentMode)
            {
                case GameModeType.VsComputer:
                    return $"1V1 VS COMPUTER ({Difficulty.ToString().ToUpper()})";
                case GameModeType.OnlineMatchmaking:
                    return $"ONLINE • {OnlineRule.ToUpper()}";
                case GameModeType.FriendRoom:
                    return IsHost ? "HOSTING PRIVATE ROOM" : $"JOINED ROOM: {RoomCode}";
                default:
                    return "CLASSIC DOMINOES";
            }
        }
    }
}

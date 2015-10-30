using System;
using System.Collections.Generic;

namespace Domain.Game
{    
    public static class Logic
    {
        public static readonly PlacePosition[][] RowPositions =
        {
            // Horizontal
            new[] { new PlacePosition(0, 0), new PlacePosition(1, 0), new PlacePosition(2, 0) },
            new[] { new PlacePosition(0, 1), new PlacePosition(1, 1), new PlacePosition(2, 1) },
            new[] { new PlacePosition(0, 2), new PlacePosition(1, 2), new PlacePosition(2, 2) },
            // Vertical
            new[] { new PlacePosition(0, 0), new PlacePosition(0, 1), new PlacePosition(0, 2) },
            new[] { new PlacePosition(1, 0), new PlacePosition(1, 1), new PlacePosition(1, 2) },
            new[] { new PlacePosition(2, 0), new PlacePosition(2, 1), new PlacePosition(2, 2) },
            // Diagonal
            new[] { new PlacePosition(0, 0), new PlacePosition(1, 1), new PlacePosition(2, 2) },
            new[] { new PlacePosition(0, 2), new PlacePosition(1, 1), new PlacePosition(2, 0) },
        };

        // return index of RowPositions, winning playerId.
        public static Tuple<int, int> FindMatchedRow(int[,] board)
        {
            for (int i = 0; i < RowPositions.Length; i++)
            {
                var rps = RowPositions[i];
                var v = board[rps[0].X, rps[0].Y];
                if (v != 0)
                {
                    if (v == board[rps[1].X, rps[1].Y] &&
                        v == board[rps[2].X, rps[2].Y])
                    {
                        return Tuple.Create(i, v);
                    }
                }
            }
            return null;
        }

        // simple AI
        public static PlacePosition DetermineMove(int[,] board, int playerId)
        {
            // If the player has two in a row, they can place a third to get three in a row.
            foreach (var rps in RowPositions)
            {
                var x = 0;
                PlacePosition emptyPosition = null;
                foreach (var pos in rps)
                {
                    var v = board[pos.X, pos.Y];
                    if (v == 0)
                        emptyPosition = pos;
                    else if (v == playerId)
                        x += 1;
                }
                if (x == 2 && emptyPosition != null)
                    return emptyPosition;
            }

            // If the opponent has two in a row, the player must play the third themselves to block the opponent.
            foreach (var rps in RowPositions)
            {
                var x = 0;
                PlacePosition emptyPosition = null;
                foreach (var pos in rps)
                {
                    var v = board[pos.X, pos.Y];
                    if (v == 0)
                        emptyPosition = pos;
                    else if (v != playerId)
                        x += 1;
                }
                if (x == 2 && emptyPosition != null)
                    return emptyPosition;
            }

            // Random pick
            var positions = new List<PlacePosition>();
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (board[x, y] == 0)
                        positions.Add(new PlacePosition(x, y));
                }
            }
            if (positions.Count > 0)
                return positions[new Random().Next(positions.Count)];

            return null;
        }
    }
}
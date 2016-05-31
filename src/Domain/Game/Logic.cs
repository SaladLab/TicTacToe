using System;
using System.Linq;
using System.Collections.Generic;

namespace Domain
{
    public static class Logic
    {
        static Logic()
        {
            var horizontals =
                Enumerable.Range(0, Rule.BoardSize).Select(
                    y => Enumerable.Range(0, Rule.BoardSize).Select(
                        x => new PlacePosition(x, y)).ToArray());
            var verticals =
                Enumerable.Range(0, Rule.BoardSize).Select(
                    x => Enumerable.Range(0, Rule.BoardSize).Select(
                        y => new PlacePosition(x, y)).ToArray());
            var diagonals =
                Enumerable.Range(0, 2).Select(
                    t => Enumerable.Range(0, Rule.BoardSize).Select(
                        x => new PlacePosition(x, t == 0 ? x : Rule.BoardSize - 1 - x)).ToArray());
            RowPositions = horizontals.Concat(verticals).Concat(diagonals).ToArray();
        }

        // array of positions that make victory
        public static readonly PlacePosition[][] RowPositions;

        // return index of RowPositions, winning playerId.
        public static Tuple<int, int> FindMatchedRow(int[,] board)
        {
            for (int i = 0; i < RowPositions.Length; i++)
            {
                var rps = RowPositions[i];
                var v = board[rps[0].X, rps[0].Y];
                if (v != 0)
                {
                    if (Enumerable.Range(1, Rule.BoardSize - 1).All(j => v == board[rps[j].X, rps[j].Y]))
                        return Tuple.Create(i, v);
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
                if (x == Rule.BoardSize - 1 && emptyPosition != null)
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
                if (x == Rule.BoardSize - 1 && emptyPosition != null)
                    return emptyPosition;
            }

            // Random pick
            var positions = new List<PlacePosition>();
            for (int x = 0; x < Rule.BoardSize; x++)
            {
                for (int y = 0; y < Rule.BoardSize; y++)
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

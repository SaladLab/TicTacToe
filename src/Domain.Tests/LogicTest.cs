using Domain.Game;
using System;
using System.Linq;
using Xunit;

namespace Domain.Tests
{
    public class LogicTest
    {
        private int[,] ParseBoard(string board)
        {
            var m = new int[Rule.BoardSize, Rule.BoardSize];
            var i = 0;
            for (int y = 0; y < Rule.BoardSize; y++)
            {
                for (int x = 0; x < Rule.BoardSize; x++)
                {
                    var c = board[i++];
                    m[x, y] = (c - '0');
                }
            }
            return m;
        }

        [Fact]
        public void Test_FindMatchedRow_FindRow()
        {
            var m = ParseBoard("021" +
                               "221" +
                               "011");

            var r = Logic.FindMatchedRow(m);

            Assert.Equal(new[] { 1, 1, 1 }, Logic.RowPositions[r.Item1].Select(p => m[p.X, p.Y]).ToArray());
            Assert.Equal(1, r.Item2);
        }

        [Fact]
        public void Test_FindMatchedRow_FindNoRow()
        {
            var m = ParseBoard("021" +
                               "221" +
                               "012");

            var r = Logic.FindMatchedRow(m);

            Assert.Equal(null, r);
        }

        [Fact]
        public void Test_DetermineMove_FirstRule()
        {
            var m = ParseBoard("001" +
                               "001" +
                               "000");

            var r = Logic.DetermineMove(m, 1);

            Assert.Equal(new PlacePosition(2, 2), r);
        }


        [Fact]
        public void Test_DetermineMove_SecondRule()
        {
            var m = ParseBoard("001" +
                               "001" +
                               "000");

            var r = Logic.DetermineMove(m, 2);

            Assert.Equal(new PlacePosition(2, 2), r);
        }
    }
}

﻿using System;
using System.Linq;

public static class LookupCompactor
{
    private static readonly int[,] lookups =
    {
        // Pawns
        {
            0,   0,   0,   0,
            150, 150, 150, 150,
            110, 110, 120, 130,
            105, 105, 110, 125,
            100, 100, 100, 130,
            105, 95,  90,  100,
            105, 110, 110, 80,
            0,   0,   0,   0
        },
        // Knights.
        {
            270, 280, 290, 290,
            280, 300, 320, 320,
            290, 320, 330, 335,
            290, 325, 335, 340,
            290, 320, 335, 340,
            290, 325, 330, 335,
            280, 300, 320, 325,
            270, 280, 290, 290,
        },
        // Bishops.
        {
            310, 320, 320, 320,
            320, 330, 330, 330,
            320, 330, 335, 340,
            320, 335, 335, 340,
            320, 330, 340, 340,
            320, 340, 340, 340,
            320, 335, 330, 330,
            310, 320, 320, 320
        },
        // Rooks.
        {
            500, 500, 500, 500,
            505, 510, 510, 510,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            500, 500, 500, 505,
        },
        // Queen.
        {
            880, 890, 890, 895,
            890, 900, 900, 900,
            890, 900, 905, 905,
            895, 900, 905, 905,
            900, 900, 905, 905,
            890, 905, 905, 905,
            890, 900, 905, 900,
            880, 890, 890, 895
        },
        // King.
        {
            870, 860, 860, 850,
            870, 860, 860, 850,
            870, 860, 860, 850,
            870, 860, 860, 850,
            880, 870, 870, 860,
            890, 880, 880, 880,
            920, 920, 900, 900,
            920, 930, 910, 900
        }
    };

    public static void Compact()
    {
        //for (int i = 0; i < 32; i += 4)
        //{
        //    Console.WriteLine($"{lookups[1, i] + 320}, {lookups[1, i + 1] + 320}, {lookups[1, i + 2] + 320}, {lookups[1, i + 3] + 320},");
        //}

        for (int j = 0; j < 6; j++)
        {
            //ulong[] table = lookups[j];
            var newTable = new ulong[8];
            for (int i = 0; i < 32; i += 4)
            {
                ushort val1 = (ushort)lookups[j, i];
                ushort val2 = (ushort)lookups[j, i + 1];
                ushort val3 = (ushort)lookups[j, i + 2];
                ushort val4 = (ushort)lookups[j, i + 3];

                newTable[i / 4]
                    = val1
                    | (ulong)val2 << 16
                    | (ulong)val3 << 32
                    | (ulong)val4 << 48;
            }

            Console.WriteLine("{\n    " + string.Join(",\n    ", newTable) + "\n},");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RC_save_editor{
    internal class DataStuff{

        [Flags]
        public enum UnitRole
        {
            None = 0,
            Spawn = 1,
            Harvester = 2,
            FrontalAttacker = 4,
            MassDestruction = 8,
            Fire = 0x10,
            Sniper = 0x20,
            Supporter = 0x40,
            Rock = 0x80,
            Robo = 0x100,
            Spawner = 0x200,
            PCXCard = 0x400,
            Building = 0x800,
            Factory = 0x1000,
            Turret = 0x2000,
            Refinery = 0x4000,
            Unit = 0x8000,
            Heal = 0x10000,
            Specialist = 0x20000,
            Skill = 0x40000,
            Engineer = 0x80000,
            Bomb = 0x100000,
            Scout = 0x200000,
            Chest = 0x400000,
            Tank = 0x800000,
            Vehicle = 0x1000000,
            Mech = 0x2000000,
            Tree = 0x4000000,
            Melee = 0x8000000,
            Research = 0x10000000,
            Crystal = 0x20000000,
            Builder = 0x40000000,
            Drop = int.MinValue,
            All = -1
        }

}}

﻿using System.Collections.Generic;
using SecretHistories.Enums;


namespace SecretHistories.Constants
{
    public class GameSpeedState
    {
        private const int levels = 3;
        private Dictionary<int,GameSpeed> CurrentGameSpeedCommands=new Dictionary<int, GameSpeed>();

        public GameSpeedState()
        {
            //1: speed control
            //2: pause state. Overrides speed control, we revert to speed control
            //3: game locked - can't un-pause with buttons or keystrokes

            for (int l = 1; l <=levels; l++)
            {
                CurrentGameSpeedCommands.Add(l, GameSpeed.DeferToNextLowestCommand);
            }

            //set base speed to normal as default
            CurrentGameSpeedCommands[1] = GameSpeed.Normal;

        }

        public void SetGameSpeedCommand(int commandPriority, GameSpeed setToSpeed)
        {
            if (commandPriority > levels || commandPriority<0)
            {
                NoonUtility.Log("Can't set a speed command priority to " + commandPriority);
                return;
            }

            //if trying to pause and already paused, then unset the pause but don't explicitly set a speed
            if (setToSpeed == GameSpeed.Paused && GetEffectiveGameSpeed() == GameSpeed.Paused)
            {
                CurrentGameSpeedCommands[commandPriority] = GameSpeed.DeferToNextLowestCommand;
                NoonUtility.Log("Trying to pause but already paused. Unsetting the pause but not explicitly setting a speed ('defer to next lowest speed command')");
            }
            else
            {
                CurrentGameSpeedCommands[commandPriority] = setToSpeed;
                NoonUtility.Log($"Setting speed {setToSpeed} as priority {commandPriority}. Effective speed is now {GetEffectiveGameSpeed()}");
            }
                

        }

        public GameSpeed GetEffectiveGameSpeed()
        {
            GameSpeed effectiveSpeed=GameSpeed.DeferToNextLowestCommand;
            foreach (var c in CurrentGameSpeedCommands)
            {

                if (c.Value != GameSpeed.DeferToNextLowestCommand)
                    effectiveSpeed = c.Value;
            }

            //if everything's unspecified, just treat the game as unpaused and running at normal speed
            if (effectiveSpeed == GameSpeed.DeferToNextLowestCommand)
                effectiveSpeed = GameSpeed.Normal;


            return effectiveSpeed;
        }

    }
}
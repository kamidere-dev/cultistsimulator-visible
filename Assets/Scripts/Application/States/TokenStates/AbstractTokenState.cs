﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.UI;
using SecretHistories.Constants;
using SecretHistories.States.TokenStates;


namespace SecretHistories.States
{
    public abstract class AbstractTokenState
    {
        public abstract bool Docked();
        public abstract bool InPlayerDrivenMotion();
        public abstract bool InSystemDrivenMotion();
        public abstract bool CanDecay();
        public abstract bool ShouldObserveRangeLimits();

        /// <summary>
        /// This is the 'finished, let's continue' state.
        /// </summary>
        /// <returns></returns>
        public AbstractTokenState GetDefaultStableState()
        {
          return new DroppedInSphereState();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecretHistories.Entities;
using SecretHistories.Fucine;

namespace SecretHistories.Commands
{
    public class AlternateRecipeExecution
    {
        public Recipe Recipe { get; set; }
        public Expulsion Expulsion { get; set; }
        public FucinePath ToPath { get; set; }

        public AlternateRecipeExecution(Recipe recipe, Expulsion expulsion, FucinePath toPath)
        {
            Recipe = recipe;
            Expulsion = expulsion;
            ToPath = toPath;
        }
    }
}

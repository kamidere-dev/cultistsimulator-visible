﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Abstract;
using SecretHistories.Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Fucine;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;

namespace SecretHistories.Assets.Scripts.Application.Spheres.Dominions
{
    [IsEmulousEncaustable(typeof(AbstractDominion))]
    public class WorldDominion : AbstractDominion
    {


        public override void Awake()
        {
            Identifier = gameObject.name;
            
            base.Awake();
        }

   

        public override Sphere TryCreateOrRetrieveSphere(SphereSpec spec)
        {
            var existingSphere = _spheres.SingleOrDefault(s => s.Id == spec.Id);
            return existingSphere; 
        }

        public override bool VisibleFor(string state)
        {
            return true;
        }



        public override bool RemoveSphere(string id, SphereRetirementType retirementType)
        {
            throw new NotImplementedException(); //maybe later
        }

        public override bool CanCreateSphere(SphereSpec spec)
        {
            return false; //maybe later.
        }
    }
}
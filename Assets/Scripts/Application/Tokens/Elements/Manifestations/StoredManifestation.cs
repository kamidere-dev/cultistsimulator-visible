﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Abstract;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Fucine;
using SecretHistories.UI;
using SecretHistories.Constants;
using SecretHistories.Constants.Events;
using SecretHistories.Ghosts;
using SecretHistories.Manifestations;
using SecretHistories.Spheres;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretHistories.Manifestations

{

#pragma warning disable 649
    [RequireComponent(typeof(RectTransform))]
    public class StoredManifestation: BasicManifestation, IManifestation
    {


        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image aspectImage;
#pragma warning restore 649



        public void Initialise(IManifestable manifestable)
        {
            name = "StoredManifestation_" + manifestable.Id;
          UpdateVisuals(manifestable,NullSphere.Create());
        }

        public void UpdateVisuals(IManifestable manifestable, Sphere sphere)
        {
            var element = Watchman.Get<Compendium>().GetEntityById<Element>(manifestable.EntityId);
       
            DisplayImage(element);
            //DisplayQuantity(quantity, hasBrightBg);
            gameObject.name = "Element - " + element.Id;
        }

        private void DisplayImage(Element element)
        {
            Sprite aspectSprite;
            aspectSprite = ResourcesManager.GetSpriteForElement(element.Icon);

            aspectImage.sprite = aspectSprite;
        }


        public void SendNotification(INotification notification)
        {
            NoonUtility.LogWarning(this.GetType().Name + " doesn't support SendNotification");
        }

        public bool HandlePointerClick(PointerEventData eventData, Token token)
        {
            return false;
        }

        public void DisplaySpheres(IEnumerable<Sphere> spheres)
        {
            NoonUtility.LogWarning(this.GetType().Name + " doesn't support DisplaySpheres");
        }

        public IGhost CreateGhost()
        {
            return NullGhost.Create(this);
        }

        public OccupiesSpaceAs OccupiesSpaceAs()
        {
            return Enums.OccupiesSpaceAs.Intangible;
        }

        public void OverrideIcon(string icon)
        {
            NoonUtility.LogWarning(this.GetType().Name + " doesn't support OverrideIcon");
        }


        public void ResetIconAnimation()
        {
   //
        }
        
        public override void Retire(RetirementVFX vfx, Action callbackOnRetired)
        {
            Destroy(gameObject);
            callbackOnRetired();
        }

        public void UpdateDecayVisuals(float lifetimeRemaining, Element element, float interval, bool currentlyBeingDragged)
        {
            //
        }

        public void Unshroud(bool instant)
        {
            //
        }

        public void Shroud(bool instant)
        {
            //
        }

        public void Emphasise()
        {
            canvasGroup.alpha = 0.3f;
        }

        public void Understate()
        {
            
        }

        public void BeginIconAnimation()
        {
        //
        }

    public bool CanAnimateIcon()
        {
           return false;
        }


        public void Highlight(HighlightType highlightType, IManifestable manifestable)
        {
 
                SoundManager.PlaySfx("TokenHover");
                aspectImage.canvasRenderer.SetColor(UIStyle.aspectHover);
   

        }

        public void Unhighlight(HighlightType highlightType, IManifestable manifestable)
        {
            aspectImage.canvasRenderer.SetColor(Color.white);
        }

        public bool NoPush { get; }
        public void DoRevealEffect(bool instant)
        {
       //
        }

        public void DoShroudEffect(bool instant)
        {
           //
        }

        public bool RequestingNoDrag => true;
        public bool RequestingNoSplit => false;


    }
}

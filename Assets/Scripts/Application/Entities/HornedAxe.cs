﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Abstract;
using SecretHistories.Assets.Scripts.Application.Entities;
using SecretHistories.Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Commands.SituationCommands;
using SecretHistories.Enums;
using SecretHistories.Fucine;
using SecretHistories.UI;
using SecretHistories.Core;
using SecretHistories.Constants;
using SecretHistories.Constants.Events;
using SecretHistories.Events;
using SecretHistories.Services;
using SecretHistories.Spheres;
using UnityEngine;

namespace SecretHistories.Entities {


    [Immanence(typeof(HornedAxe))]
    public class HornedAxe {

        public bool EnableAspectCaching = false; //this was breaking something in stacked cards
        
        private readonly HashSet<ISphereCatalogueEventSubscriber> _subscribers = new HashSet<ISphereCatalogueEventSubscriber>();
        private AspectsDictionary _tabletopAspects = null;
        private AspectsDictionary _allAspectsExtant = null;
        private bool _tabletopAspectsDirty = true;
        private bool _allAspectsExtantDirty = true;
        private List<Situation> _currentSituations;
        private readonly HashSet<Way> _registeredWays = new HashSet<Way>();
        private readonly HashSet<Sphere> _registeredSpheres=new HashSet<Sphere>(); //Use this to find, eg, all World spheres. Don't use it for Fucine pathing!
        private FucinePath _defaultSpherePath;
        private Dictionary<string, string> _alternativeDefaultWorldSpherePaths;

        public HornedAxe()
        {
            Reset();
        }

        

        public void Reset()
        {
            _currentSituations = new List<Situation>();
            foreach (var s in new List<Sphere>(_registeredSpheres))
            {
                if (!s.PersistBetweenScenes)
                    DeregisterSphere(s);
            }

            _subscribers.Clear();

         
        }

        public void PopulateDefaultSpherePaths()
        {
            var dictum = Watchman.Get<Compendium>().GetSingleEntity<Dictum>();

            var pathFromDictum = dictum.DefaultWorldSpherePath;
            var alternativePathsFromDictum = dictum.AlternativeDefaultWorldSpherePaths;
            _defaultSpherePath = pathFromDictum;
            _alternativeDefaultWorldSpherePaths = alternativePathsFromDictum;
        }

        public FucinePath GetDefaultSpherePath()
        {

            if (_defaultSpherePath == null)
                PopulateDefaultSpherePaths();

            return _defaultSpherePath;
        }
        public FucinePath GetDefaultSpherePath(OccupiesSpaceAs forOccupiesSpaceAs)
        {

            if (_defaultSpherePath == null || _alternativeDefaultWorldSpherePaths==null)
                PopulateDefaultSpherePaths();

            _alternativeDefaultWorldSpherePaths.TryGetValue(forOccupiesSpaceAs.ToString().ToLower(), //ugh. we need the enums in the dict really
                out var specificPath);

            if (specificPath == null)
                return _defaultSpherePath;

            return new FucinePath(specificPath);

        }


        public Sphere GetDefaultSphere(OccupiesSpaceAs forOccupiesSpaceAs)
        {
            var defaultSphere = GetSphereByAbsolutePath(GetDefaultSpherePath(forOccupiesSpaceAs));
            return defaultSphere;
        }


        public HashSet<Sphere> GetSpheres() {
            return new HashSet<Sphere>(_registeredSpheres);
        }

        public List<Sphere> GetSpheresWhichAllowDragging()
        {
            //cache?
            var spheresWhichCanBeDraggedFrom = _registeredSpheres.Where(s => !s.Defunct && s.AllowDrag);
            return spheresWhichCanBeDraggedFrom.ToList();
        }

        public List<Sphere> GetSpheresWhichAllowAmbientAnimations()
        {
            //cache?
            var spheresWhichCanBeDraggedFrom = _registeredSpheres.Where(s => s.AllowAmbientAnimations && !s.Defunct);
            return spheresWhichCanBeDraggedFrom.ToList();
        }

        public IEnumerable<Sphere> GetSpheresOfCategory(SphereCategory category)
        {
            return _registeredSpheres.Where(c => c.SphereCategory == category);
        }

        public IEnumerable<Sphere> GetTraversableSpheres()
        {
            return _registeredSpheres.Where(s=>s.Traversable);
        }


        public void RegisterSphere(Sphere sphere) {
            
            if(sphere.GoverningSphereSpec==null)
                NoonUtility.LogWarning($"Registered a sphere, {sphere.gameObject.name} , container {sphere.GetContainer().Id}, with no spec");
            else if( string.IsNullOrEmpty(sphere.Id))
                NoonUtility.LogWarning($"Registered a sphere, {sphere.gameObject.name} , container {sphere.GetContainer().Id}, with an empty id");

            _registeredSpheres.Add(sphere);
        }

        public void DeregisterSphere(Sphere sphere) {

            _registeredSpheres.Remove(sphere);
        }

        public void RegisterWay(Way way)
        {
            _registeredWays.Add(way);
        }

        public void DeregisterWay(Way way)
        {
            _registeredWays.Remove(way);
        }

        public List<Way> GetWaysLinkedToSphere(Sphere s)
        {
            var linkedWays = _registeredWays.Where(w => w.LinkedSpheres.Contains(s));
            return new List<Way>(linkedWays);
        }

        public Way GetNextWayInRoute(Sphere fromSphere, Sphere toSphere)
        {
            throw new NotImplementedException();
        }

        public void Subscribe(ISphereCatalogueEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
         }

        public void Unsubscribe(ISphereCatalogueEventSubscriber subscriber)
        {
            _subscribers.Remove(subscriber);
        }


        public Sphere GetSphereByPath(FucinePath relativePath, IHasFucinePath relativeTo)
        {
            var constructedPath = relativeTo.GetAbsolutePath().AppendingPath(relativePath);
            return GetSphereByAbsolutePath(constructedPath);
        }

        public Sphere GetSphereByPath(Sphere currentSphere, FucinePath spherePath)
        {
            
            if (spherePath == FucinePath.Current())
                return currentSphere;
            return GetSphereByAbsolutePath(spherePath);
        }
        //Transition to the above method wherever possible, and eventually go for a warning and a DefaultSphere with the one below

        public Sphere GetSphereByAbsolutePath(FucinePath spherePath)
        {
            if (spherePath.IsWild())
                return FindFirstOrDefaultSphereByWildPath(spherePath);

            if(!spherePath.IsAbsolute())
               throw new ApplicationException($"trying to find a sphere with sphere path '{spherePath.ToString()}', but that's not an absolute path or a wildcard path, and no context was provided");

            if(spherePath.GetEndingPathPart().Category==FucinePathPart.PathCategory.Root)
                throw new ApplicationException($"trying to find a sphere with sphere path '{spherePath.ToString()}', which seems to be a bare root path");

            if (spherePath.IsPathToSphereInRoot())
                return GetSphereInRootByPath(spherePath);

            //either it's a token path - in which case return the fallback sphere - 
            if (spherePath.GetEndingPathPart().Category == FucinePathPart.PathCategory.Token)
                return GetFallbackSphere(spherePath);

            //or it's a sphere not in the root, i.e. in a token payload.
            var targetSphereId = spherePath.GetEndingPathPart().GetId();
            var candidateTokenPath = spherePath.GetTokenPath();
            var tokenThatShouldContainSphere= GetTokenByPath(candidateTokenPath);

            if(tokenThatShouldContainSphere!=null && tokenThatShouldContainSphere.Payload!=null) 
            {
                var sphereFound = tokenThatShouldContainSphere.Payload.GetSphereById(targetSphereId);
                    if (sphereFound != null)
                        return sphereFound;
                
            }
            return GetFallbackSphere(spherePath);

        }

        private Sphere GetSphereInRootByPath(FucinePath spherePath)
        {
            try
            {
                var specifiedSphere = FucineRoot.Get().Spheres.SingleOrDefault(c => c.GetAbsolutePath() == spherePath);
                if (specifiedSphere == null)
                {
                    return GetFallbackSphere(spherePath);
                }

                return specifiedSphere;

            }
            catch (Exception e)
            {
                NoonUtility.LogWarning($"Error retrieving sphere with path {spherePath}: {e.Message}");
                return Watchman.Get<Limbo>();
            }
        }

        private Sphere GetFallbackSphere(FucinePath spherePath)
        {
            if (spherePath == GetDefaultSpherePath(OccupiesSpaceAs.Intangible))
            {
                NoonUtility.LogWarning(
                    $"Can't find sphere with path {spherePath}, nor a default world sphere, so returning limbo");
                return
                    Watchman.Get<Limbo>(); //we can't find the default world sphere, so no point getting stuck in an infinite loop - just return limbo.
            }
            else
            {
                NoonUtility.LogWarning($"Can't find sphere with path {spherePath}, so returning a default world sphere");
                return GetDefaultSphere(OccupiesSpaceAs.Unknown); //no longer limbo; let's let people recover things
            }
        }

        public void OnAnySphereChanged(SphereChangedArgs args)
        {
            foreach (var s in new List<ISphereCatalogueEventSubscriber>(_subscribers))
                if (s.Equals(null))
                    Unsubscribe(s);
                else
                    s.OnSphereChanged(args);
        }


        public void OnTokensChangedForAnySphere(SphereContentsChangedEventArgs args)
        {
            NotifyAspectsDirty();

            foreach(var s in new List<ISphereCatalogueEventSubscriber>(_subscribers))
                if(s.Equals(null))
                    Unsubscribe(s);
                else
                    s.OnTokensChanged(args);
        }

        public void OnTokenInteractionInAnySphere(TokenInteractionEventArgs args)
        {
            foreach (var s in new List<ISphereCatalogueEventSubscriber>(_subscribers))

                if (s.Equals(null))
                    Unsubscribe(s);
                else
                    s.OnTokenInteraction(args);
        }

        public List<Token> FindTokensWithAspectsInWorld(Dictionary<string, string> withAspects)
        {
            List<Token> matchingTokens = new List<Token>();
            foreach(var ws in GetSpheresOfCategory(SphereCategory.World))
            {
                var validTokensToCheck = ws.Tokens.Where(t => t.IsValid());

                foreach (var tokenToCheck in validTokensToCheck)
                {
                    
                    bool mismatch = false;
                    foreach (var criterion in withAspects)
                    {
                        if(mismatch == true)
                            continue; //if we already found a mismatch, don't bother checking the others
                        
                        bool matches = Recipe.CheckRequirementsSatisfiedForContext(tokenToCheck.GetAspects(true), criterion);
                        if(!matches)
                            mismatch=true;
                    }
                    if(!mismatch)
                        matchingTokens.Add(tokenToCheck);
                }
            }

            return matchingTokens;
        }


        public AspectsInContext GetAspectsInContext(AspectsDictionary aspectsInSituation,AspectsDictionary aspectsNearby)
        {
            if (aspectsNearby == null)
                aspectsNearby = new AspectsDictionary();

            if (!EnableAspectCaching)
            {
                _tabletopAspectsDirty = true;
                _allAspectsExtantDirty = true;
            }

            if (_tabletopAspectsDirty)
            {
                if (_tabletopAspects == null)
                    _tabletopAspects = new AspectsDictionary();
                else
                    _tabletopAspects.Clear();

                List<ElementStack> tabletopStacks=new List<ElementStack>();


                var tabletopContainers =
                    _registeredSpheres.Where(tc => tc.SphereCategory == SphereCategory.World);

                foreach(var tc in tabletopContainers)
                    tabletopStacks.AddRange(tc.GetElementStacks().Select(es=>es as ElementStack));

                
                foreach (var tabletopStack in tabletopStacks)
                {
                    AspectsDictionary stackAspects = tabletopStack.GetAspects();

                    //TENTATIVELY REMOVING ALL THIS BECAUSE OF THE BIG DECISION TO MULTIPLY ALL ASPECTS
                    //AspectsDictionary multipliedAspects = new AspectsDictionary();
                    ////If we just count aspects, a stack of 10 cards only counts them once. I *think* this is the only place we need to worry about this rn,
                    ////but bear it in mind in case there's ever a similar issue inside situations <--there is! if multiple cards are output, they stack.
                    ////However! To complicate matters, if we're counting elements rather than aspects, there is already code in the stack to multiply aspect * quality, and we don't want to multiply it twice
                    //foreach (var aspect in stackAspects)
                    //{

                    //    if (aspect.Key == tabletopStack.EntityId)
                    //        multipliedAspects.Add(aspect.Key, aspect.Value);
                    //    else
                    //        multipliedAspects.Add(aspect.Key, aspect.Value * tabletopStack.Quantity);
                    //}
                    //_tabletopAspects.CombineAspects(multipliedAspects);
                    _tabletopAspects.CombineAspects(stackAspects);
                }


                if (EnableAspectCaching)
                {
                    _tabletopAspectsDirty = false;		// If left dirty the aspects will recalc every frame
                    _allAspectsExtantDirty = true;      // Force the aspects below to recalc
                }
            }

            if (_allAspectsExtantDirty)
            {
                if (_allAspectsExtant == null)
                    _allAspectsExtant = new AspectsDictionary();
                else
                    _allAspectsExtant.Clear();


                foreach (var s in GetRegisteredSituations())
                {
                    var stacksInSituation = s.GetElementTokensInSituation().Select(e=>e.Payload as ElementStack);
                    foreach (var situationStack in stacksInSituation)
                    {
                        AspectsDictionary stackAspects = situationStack.GetAspects();
                       // AspectsDictionary multipliedAspects = new AspectsDictionary();
                        //TENTATIVELY REMOVING ALL THIS BECAUSE OF THE BIG DECISION TO MULTIPLY ALL ASPECTS
                        //See notes above. We need to multiply aspects to take account of stack quantities here too.
                        //foreach (var aspect in stackAspects)
                        //{

                        //    if (aspect.Key == situationStack.EntityId)
                        //        multipliedAspects.Add(aspect.Key, aspect.Value);
                        //    else
                        //        multipliedAspects.Add(aspect.Key, aspect.Value * situationStack.Quantity);
                        //}
                        //_allAspectsExtant.CombineAspects(multipliedAspects);
                        _allAspectsExtant.CombineAspects(stackAspects);
                    }

                }
                _allAspectsExtant.CombineAspects(_tabletopAspects);

                if (EnableAspectCaching)
                    _allAspectsExtantDirty = false;     // If left dirty the aspects will recalc every frame
            }


            AspectsInContext aspectsInContext = new AspectsInContext(aspectsInSituation, aspectsNearby, _tabletopAspects, _allAspectsExtant);

            return aspectsInContext;

        }

        public int PurgeElement(string elementId, int maxToPurge)
        {
            var compendium = Watchman.Get<Compendium>();

            Element elementToPurge = compendium.GetEntityById<Element>(elementId);

            var worldSpheres = GetSpheresOfCategory(SphereCategory.World);
            foreach (var worldSphere in worldSpheres)
            {
                if (maxToPurge <= 0)
                    return maxToPurge;

                maxToPurge = worldSphere.TryPurgeStacks(elementToPurge, maxToPurge);

            }

   

            foreach (var s in GetRegisteredSituations())
            {
                if (maxToPurge <= 0)
                    return maxToPurge;
           
                maxToPurge = s.TryPurgeStacks(elementToPurge, maxToPurge);

            }

            return maxToPurge;
        }

        public void NotifyAspectsDirty()
        {
            _tabletopAspectsDirty = true;
        }


        public List<Situation> GetRegisteredSituations()
        {
            return new List<Situation>(_currentSituations);
        }


        public void RegisterSituation(Situation situation)
        {
            _currentSituations.Add(situation);
        }

        public void DeregisterSituation(Situation situation)
        {
            _currentSituations.Remove(situation);
        }

        public IEnumerable<Situation> GetSituationsWithVerbOfType(Type verbType)
        {
            return _currentSituations.Where(situation => situation.Verb.GetType() == verbType);
        }

        public IEnumerable<Situation> GetSituationsWithVerbOfActionId(string actionId)
        {
            return _currentSituations.Where(situation => situation.Verb.Id == actionId);
        }



        public void HaltSituation(string toHaltId, int maxToHalt)
        {

            int i = 0;
            //Halt the verb if the actionId matches BEARING IN MIND WILDCARD

            if (toHaltId.Contains('*'))
            {
                string wildcardToDelete = toHaltId.Remove(toHaltId.IndexOf('*'));

                foreach (var s in GetRegisteredSituations())
                {
                    if (s.Verb.Id.StartsWith(wildcardToDelete))
                    {
                        s.ApplyExoticEffect(ExoticEffect.Halt);
                        i++;
                    }

                    if (i >= maxToHalt)
                        break;
                }
            }

            else
            {
                foreach (var s in GetRegisteredSituations())
                {
                    if (s.Verb.Id == toHaltId.Trim())
                    {
                        s.ApplyExoticEffect(ExoticEffect.Halt);
                        i++;
                    }
                    if (i >= maxToHalt)
                        break;
                }
            }
        }

        public void PurgeSituation(string toDeleteId, int maxToPurge)
        {

            int i = 0;
            //Delete the verb if the actionId matches BEARING IN MIND WILDCARD

            if (toDeleteId.Contains('*'))
            {
                string wildcardToDelete = toDeleteId.Remove(toDeleteId.IndexOf('*'));

                foreach (var s in GetRegisteredSituations())
                {
                    if (s.Verb.Id.StartsWith(wildcardToDelete))
                    {
                        s.ApplyExoticEffect(ExoticEffect.Purge);
                        i++;
                    }

                    if (i >= maxToPurge)
                        break;
                }
            }

            else
            {
                foreach (var s in GetRegisteredSituations())
                {
                    if (s.Verb.Id == toDeleteId.Trim())
                    {
                        s.ApplyExoticEffect(ExoticEffect.Purge);
                        i++;
                    }
                    if (i >= maxToPurge)
                        break;
                }
            }
        }

        //public IEnumerable<Token> GetAnimatables()
        //{
        //    var situationTokens = GetRegisteredSituations().Select(s => s.situationAnchor as Token);

        //    return situationTokens.Where(s => s.CanAnimateArt());
        public Token GetTokenByPath(FucinePath tokenInSpherePath)
        {

            var spherePath = tokenInSpherePath.GetSpherePath();
            var tokenPath = tokenInSpherePath.GetTokenPath();
            var pathTokenId = tokenPath.GetEndingPathPart().GetId();
            Token tokenToReturn;
            if(spherePath.IsRoot())
            {
                //I originally decided there would never be a token directly in the root. There *is*,
                //though, with the Mansus and increasingly with other things like terrain features, so the rule needs rethinking
                //I need to make sure it's consistent, though.
                tokenToReturn = FindSingleOrDefaultTokenById(pathTokenId);
            }
            else
            
            {
                    var sphere = GetSphereByAbsolutePath(spherePath);
                    tokenToReturn = sphere.Tokens.SingleOrDefault(t => t.MatchesPathTokenId(pathTokenId));
            }
            return tokenToReturn;

        }

        public Sphere FindFirstOrDefaultSphereByWildPath(FucinePath wildPath)
        {
            return FindFirstOrDefaultSphereByWildPath(wildPath, NullSphere.Create());
        }

        public Sphere FindFirstOrDefaultSphereByWildPath(FucinePath wildPath,Sphere defaultResult)
        {
            
            foreach (var s in _registeredSpheres)
            {
                if (s.GetWildPath() == wildPath)
                    return s;
            }

            return defaultResult;
        }



        public Token FindSingleOrDefaultTokenById(string uniqueTokenId)
        {
            return FindSingleOrDefaultTokenById(uniqueTokenId, null);
        }

        public Token FindSingleOrDefaultTokenById(string uniqueTokenId,Token defaultResult)
        {
            foreach (var s in _registeredSpheres)
            {
                var tokens = s.GetTokensWhere(t => t.PayloadId == uniqueTokenId);
                
                if (tokens.Any())
                {
                    var tokensList = tokens.ToList(); //avoiding multiple enumeration warning
                    if(tokensList.Count>1)
                    {
                        NoonUtility.LogWarning(
                            $"Found more than one token with id {uniqueTokenId} in sphere {s.GetAbsolutePath()} . This should never happen, but here's a warning in case it did. Returning the first token with id {uniqueTokenId}");
                        return tokensList.First();
                    }

                    //>0 and <=1: 1

                    return tokensList.Single();
                }

            }

            return defaultResult;
        }


        public void SetDefaultSpherePath(FucinePath fucinePath)
        {
            _defaultSpherePath = fucinePath;
        }
    }
}

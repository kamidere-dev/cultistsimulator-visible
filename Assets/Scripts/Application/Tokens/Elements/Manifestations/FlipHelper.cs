﻿using System.Collections;
using UnityEngine;

namespace SecretHistories.Manifestations
{
    public class FlipHelper
    {
        public enum TargetOrientation
        {
            FaceUp=0,
            FaceDown=180
        }

        private readonly MonoBehaviour _flippee;
        private Coroutine _turnCoroutine;
        private TargetOrientation? _targetOrientation;
        private TargetOrientation? _currentOrientation;
        
        public bool FlipInProgress => _turnCoroutine != null;

        public TargetOrientation? CurrentOrientation
        {
            get => _currentOrientation;
            set => _currentOrientation = value;
        }

        public FlipHelper(MonoBehaviour flippee)
        {
            _flippee = flippee;
            }

        public void Flip(TargetOrientation targetOrientation, bool instant)
        {
            if (!instant)
                SoundManager.PlaySfx("CardTurnOver");

            _targetOrientation = targetOrientation;

  
            if (_flippee.gameObject.activeInHierarchy == false || instant)
            {
                FinishFlip();
            }

            if (_turnCoroutine != null)
                _flippee.StopCoroutine(_turnCoroutine);

            _turnCoroutine = _flippee.StartCoroutine(DoTurn());
        }


        public void FinishFlip()
        {
            if (_targetOrientation == null)
                return;

            if (_flippee == null || _flippee.Equals(null))
            {
                NoonUtility.LogWarning($"Trying to finish flipping/shrouding a card to {_targetOrientation}, but the flippee is null. This shouldn't happen, but maybe it just did!");
                return;
            }


            _flippee.transform.localRotation = Quaternion.Euler(0f, (float)_targetOrientation, 0f);
            if(_turnCoroutine!=null)
            {
                _flippee.StopCoroutine(_turnCoroutine);
                _turnCoroutine = null;
            }
            CurrentOrientation = _targetOrientation;
            _targetOrientation = null;
        }


        private IEnumerator DoTurn()
        {
            if (_targetOrientation != null)
            {
                float time = 0f;
                
                float currentAngle = _flippee.transform.localEulerAngles.y;
                float duration = Mathf.Abs((float)_targetOrientation - currentAngle) / 900f;
                

                while (time < duration && _targetOrientation!=null)
                {
                    time += Time.deltaTime;
                    _flippee.transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(currentAngle, (float)_targetOrientation, time / duration), 0f);
                    yield return null;
                }
            }

            FinishFlip();
        }

   
    }
}
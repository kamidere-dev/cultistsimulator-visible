﻿using System;
using SecretHistories.Constants;
using SecretHistories.Entities;
using TMPro;
using UnityEngine;
using SecretHistories.UI;
using SecretHistories.Fucine;
using SecretHistories.Services;

using SecretHistories.Enums.UI;
using UnityEngine.UI; // For accessing high contrast mode


public class Babelfish : MonoBehaviour,ISettingSubscriber
{
	[SerializeField] private string						locLabel;
#pragma warning disable 649
    [Tooltip("Which font set should this text use?\nFont sets assigned in LanguageManager")]
	[SerializeField] private LanguageManager.eFontStyle fontStyle;
    [Tooltip("Force this string to use the font for a specific language (empty by default)")]
    [SerializeField] private string						forceFontLanguage;
#pragma warning restore 649
	[SerializeField] private bool						highContrastPermissibleInContext = true;
	[SerializeField] private bool						highContrastBold = true;
    [SerializeField] private bool                       forceBold = false;


	private Color		defaultColor;
	private FontStyles	defaultStyle;
    private TMP_Text _tmpText;

    private bool _initialised = false;

    protected TMP_Text GetTextComponent()
    {
        if (_tmpText == null)
        {
            _tmpText = GetComponent<TMP_Text>();

            try
            {
            
            defaultColor = _tmpText.color;
            defaultStyle = _tmpText.fontStyle;
            }
            catch (Exception e)
            {
                NoonUtility.Log($"Can't find or set text component in Babelfish loclabel {locLabel}: {e.ToString()}");
                throw;
            }
        }
        return _tmpText;
    }

    private bool HighContrastEnabledInGlobalSettings=false;

    private void OnEnable()
    {
        if (!_initialised)
            Initialise();
    }

    private void Initialise()
    {
        var concursum = Watchman.Get<Concursum>();

        concursum.ChangingCulture.AddListener(OnCultureChanged);
        concursum.ContentUpdatedEvent.AddListener(OnContentUpdated);

        var highContrastSetting = Watchman.Get<Compendium>().GetEntityById<Setting>(NoonConstants.HIGHCONTRAST);
        if (highContrastSetting != null)
        {
            highContrastSetting.AddSubscriber(this);
            WhenSettingUpdated(highContrastSetting.CurrentValue);
        }
        else
            NoonUtility.Log("Missing setting entity: " + NoonConstants.HIGHCONTRAST);

        _initialised = true;

    }

    public void OnDestroy()
    {
        var concursum = Watchman.Get<Concursum>();

        concursum.ChangingCulture.RemoveListener(OnCultureChanged);
        concursum.ContentUpdatedEvent.RemoveListener(OnContentUpdated);

        var highContrastSetting = Watchman.Get<Compendium>().GetEntityById<Setting>(NoonConstants.HIGHCONTRAST);
        if (highContrastSetting != null)
        {
            highContrastSetting.RemoveSubscriber(this);
        }
    }


    public void SetValuesForCurrentCulture()
    {
        string currentCultureId = Watchman.Get<Config>().GetConfigValue(NoonConstants.CULTURE_SETTING_KEY);

        var currentCulture = Watchman.Get<Compendium>().GetEntityById<Culture>(currentCultureId);

        SetValuesFromCulture(currentCulture);
    }

    public void SetValuesFromCulture(Culture culture)
    {
        

        ILocStringProvider lm = Watchman.Get<ILocStringProvider>();

        if (lm == null)
            lm = new NullLocStringProvider();
        

        string fontscript;
        try
        {

        

        if (!string.IsNullOrEmpty(forceFontLanguage))
            fontscript = forceFontLanguage;
        else
            fontscript = culture.FontScript;

        TMP_FontAsset font = lm.GetFont(fontStyle, fontscript);
        if (font != null)
        {
            GetTextComponent().font = font;
        }

        // If using a specific font material, map the material to the
        // appropriate font texture atlas, then set the font asset's material.
        Material fontMaterial = lm.GetFontMaterial(fontStyle);
        if (fontMaterial != null)
        {
            fontMaterial.SetTexture("_MainTex", GetTextComponent().font.material.mainTexture);
            GetTextComponent().fontMaterial = fontMaterial;
        }

        // Localization label: only applies if set.
        if (!string.IsNullOrEmpty(locLabel))
        {
            GetTextComponent().text = lm.Get(locLabel);
        }

        SetFontStyle(culture, lm);

        }
        catch (Exception e)
        {
            //intermittent error where GetTextComponent throws a nullref exception.
            //This is a guard clause to deal with something like this so at least it doesn't bring the game down.
            NoonUtility.LogException(e);
        }
    }

    public void WhenSettingUpdated(object newValue)
    {
        HighContrastEnabledInGlobalSettings = ((newValue is float ? (float)newValue : 0) > 0.5f);
        string currentCultureId = Watchman.Get<Config>().GetConfigValue(NoonConstants.CULTURE_SETTING_KEY); 
        var currentCulture = Watchman.Get<Compendium>().GetEntityById<Culture>(currentCultureId);
        SetValuesFromCulture(currentCulture);

        ILocStringProvider lm = Watchman.Get<ILocStringProvider>();

        if (lm == null)
            lm = new NullLocStringProvider();

        SetFontStyle(currentCulture,lm);
    }

    private void SetFontStyle(Culture culture, ILocStringProvider lm)
    {
        if (highContrastPermissibleInContext)
        {
            //highContrastBold = true;	// Force all text to go bold

            var highContrastEnabled = Watchman.Get<Config>().GetConfigValueAsInt(NoonConstants.HIGHCONTRAST);

            if (highContrastEnabled!=null && highContrastEnabled>0)
            {
                Color light = lm.HighContrastLight;
                Color dark = lm.HighContrastDark;
                light.a = 1.0f; // ensure color is opaque
                dark.a = 1.0f;
                GetTextComponent().color = defaultColor.grayscale > 0.5f ? light : dark;
                if (highContrastBold)
                    GetTextComponent().fontStyle |= FontStyles.Bold;
            }
            else
            {
                GetTextComponent().color = defaultColor;
                if (highContrastBold)
                    GetTextComponent().fontStyle = defaultStyle;
            }
        }

        // Always disable bold for Chinese, since it can make the text
        // unreadable
        if (!culture.BoldAllowed)
        {
            GetTextComponent().fontStyle &= ~FontStyles.Bold;
        }

        if (forceBold)
        {
            GetTextComponent().fontStyle |= FontStyles.Bold;
        }
    }


    public void UpdateLocLabel( string label )	
	{
        
		locLabel = label;
        SetValuesForCurrentCulture();
	}


    public virtual void OnContentUpdated(ContentUpdatedArgs args)
    {
        SetValuesForCurrentCulture();
    }

	public virtual void OnCultureChanged(CultureChangedArgs args)
    {
     SetValuesFromCulture(args.NewCulture);
    }


}

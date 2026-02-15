using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;

public class LanguageToggle : MonoBehaviour
{
    bool isChanging = false;

    public void ToggleLanguage()
    {
        if (!isChanging)
            StartCoroutine(SetNextLocale());
    }

    IEnumerator SetNextLocale()
    {
        isChanging = true;

        // Ensure localization system is initialized
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0)
        {
            isChanging = false;
            yield break;
        }

        var current = LocalizationSettings.SelectedLocale;
        int index = locales.IndexOf(current);
        if (index < 0) index = 0;

        int nextIndex = (index + 1) % locales.Count;
        LocalizationSettings.SelectedLocale = locales[nextIndex];

        isChanging = false;
    }
}
import en from './en.json'
import no from './no.json'

export type TranslationWithArgumentsType = { text: string; args: number }
export const defaultLanguage = 'en'
export const languageOptions: { [key: string]: string } = { en: 'English', no: 'Norsk' }
export const allLanguageDictionariesFromJson: { [key: string]: Record<string, string | TranslationWithArgumentsType> } =
    { en, no }
export const allLanguageDictionaries: { [key: string]: Record<string, TranslationWithArgumentsType> } = {}
Object.keys(allLanguageDictionariesFromJson).forEach((key) => {
    const thisDictionaryFromJson = allLanguageDictionariesFromJson[key]
    const languageDictionary: { [key: string]: TranslationWithArgumentsType } = {}
    Object.keys(thisDictionaryFromJson).forEach((key) => {
        if ((thisDictionaryFromJson[key] as TranslationWithArgumentsType).args)
            languageDictionary[key] = thisDictionaryFromJson[key] as TranslationWithArgumentsType
        else languageDictionary[key] = { text: thisDictionaryFromJson[key] as string, args: 0 }
    })
    allLanguageDictionaries[key] = languageDictionary
})

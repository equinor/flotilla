import en from './en.json'
import no from './no.json'

export const defaultLanguage = 'en'
export const languageOptions: { [key: string]: string } = { en: 'English', no: 'Norsk' }
export const allLanguageDictionaries: { [key: string]: {} } = { en, no }

export const LanguageShort = (languageLong: string) => {
    const langKey = (Object.keys(languageOptions) as (keyof typeof languageOptions)[]).find((key) => {
        return languageOptions[key] === languageLong
    })
    const langShort = langKey ? (langKey as string) : defaultLanguage
    return langShort
}

import en from './en.json'
import no from './no.json'

export const defaultLanguage = 'no'
export const languageOptions: { [key: string]: string } = { en: 'English', no: 'Norsk' }
export const allLanguageDictionaries: { [key: string]: Record<string, string> } = { en, no }

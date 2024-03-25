import { createContext, FC, useContext, useState } from 'react'
import { defaultLanguage, allLanguageDictionaries, languageOptions, TranslationWithArgumentsType } from 'language'

interface ILanguageContext {
    language: string
    textDictionary: { [text: string]: TranslationWithArgumentsType }
    switchLanguage: (newLanguage: string) => void
    TranslateText: (str: string, args?: string[]) => string
}

interface Props {
    children: React.ReactNode
}

const defaultLanguageInterface = {
    language: defaultLanguage,
    textDictionary: allLanguageDictionaries[defaultLanguage],
    switchLanguage: (newLanguage: string) => {},
    TranslateText: (str: string) => '',
}

export const LanguageContext = createContext<ILanguageContext>(defaultLanguageInterface)

export const LanguageProvider: FC<Props> = ({ children }) => {
    const prevLanguage = window.localStorage.getItem('flotilla-language')
    const [language, setLanguage] = useState(prevLanguage || defaultLanguage)

    if (!Object.keys(languageOptions).includes(language)) setLanguage(defaultLanguage)

    const textDictionary = allLanguageDictionaries[language]
    const switchLanguage = (newLanguage: string) => {
        setLanguage(newLanguage)
        window.localStorage.setItem('flotilla-language', newLanguage)
    }

    const TranslateText = (str: string, args?: string[]): string => {
        const translationMapping = textDictionary[str]
        if (!translationMapping) {
            console.warn(`Translation issue: "${str}" has no translation to language "${language}"`)
            return str
        }

        const translationText = translationMapping.text
        const translationArgs = translationMapping.args

        if (translationArgs === 0) return translationText

        if (!args) {
            console.warn(`Translation issue: "${str}" requires "${translationArgs}" arguments but was provided none`)
            return str
        }

        if (args.length !== translationArgs) {
            console.warn(
                `Translation issue: "${str}" requires "${translationArgs}" arguments but was provided "${args.length}"`
            )
            return str
        }

        return translationText.replaceAll(/\{\d\}/g, (match) => {
            match = match.slice(1, match.length - 1) // Removes { and }
            return args[+match]
        })
    }

    return (
        <LanguageContext.Provider
            value={{
                language,
                textDictionary,
                switchLanguage,
                TranslateText,
            }}
        >
            {children}
        </LanguageContext.Provider>
    )
}

export const useLanguageContext = () => useContext(LanguageContext)

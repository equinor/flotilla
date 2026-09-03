import { createContext, FC, useContext, useState } from 'react'
import { defaultLanguage, allLanguageDictionaries, languageOptions } from 'language'

interface ILanguageContext {
    language: string
    textDictionary: { [text: string]: string }
    switchLanguage: (newLanguage: string) => void
    TranslateText: (str: string, args?: string[]) => string
}

interface Props {
    children: React.ReactNode
}

const defaultLanguageInterface = {
    language: defaultLanguage,
    textDictionary: allLanguageDictionaries[defaultLanguage],
    switchLanguage: () => {},
    TranslateText: () => '',
}

const LanguageContext = createContext<ILanguageContext>(defaultLanguageInterface)

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
        const translationText = textDictionary[str]
        if (!translationText) {
            console.warn(`Translation issue: "${str}" has no translation to language "${language}"`)
            return str
        }

        // This regex matches a pair of brackets with a number inside it. No number can start with 0, unless it is just 0
        const bracketWithNumberRegex: RegExp = /\{[0]\}|\{[1-9][\d]*\}/g
        const numberOfArgs = translationText.match(bracketWithNumberRegex)?.length ?? 0

        if (numberOfArgs === 0) return translationText

        if (!args) {
            console.warn(`Translation issue: "${str}" requires "${numberOfArgs}" arguments but was provided none`)
            return str
        }

        if (args.length !== numberOfArgs) {
            console.warn(
                `Translation issue: "${str}" requires "${numberOfArgs}" arguments but was provided "${args.length}"`
            )
            return str
        }

        return translationText.replaceAll(bracketWithNumberRegex, (match) => {
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

import { createContext, FC, useContext, useState } from 'react'
import { defaultLanguage, allLanguageDictionaries, languageOptions } from '../../language'

interface ILanguageContext {
    language: string
    textDictionary: { [text: string]: string }
    switchLanguage: (newLanguage: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultLanguageInterface = {
    language: defaultLanguage,
    textDictionary: allLanguageDictionaries[defaultLanguage],
    switchLanguage: (newLanguage: string) => {},
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

    return (
        <LanguageContext.Provider
            value={{
                language,
                textDictionary,
                switchLanguage,
            }}
        >
            {children}
        </LanguageContext.Provider>
    )
}

export const useLanguageContext = () => useContext(LanguageContext)

export const TranslateText = (id: string): string => {
    const languageContext = useContext(LanguageContext)
    if (languageContext.textDictionary[id]) {
        return languageContext.textDictionary[id]
    }
    console.warn(`Translation issue: "${id}" has no translation to language "${languageContext.language}"`)
    return id
}

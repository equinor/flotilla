import { createContext, FC, useContext, useState } from 'react'
import { dictionaryList } from '../../language'

interface ILanguageContext {
    language: string
    textDictionary: { [text: string]: string }
    switchLanguage: (newLanguage: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultLanguage = {
    language: 'en',
    textDictionary: dictionaryList.en,
    switchLanguage: (newLanguage: string) => {},
}

export const LanguageContext = createContext<ILanguageContext>(defaultLanguage)

export const LanguageProvider: FC<Props> = ({ children }) => {
    const prevLanguage = window.localStorage.getItem('rcml-lang')
    const [language, setLanguage] = useState(prevLanguage || defaultLanguage.language)

    const textDictionary =
        language === 'en' || language === 'no' ? dictionaryList[language] : defaultLanguage.textDictionary
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

export function Text(id: string) {
    const languageContext = useContext(LanguageContext)
    if (languageContext.textDictionary[id]) {
        return languageContext.textDictionary[id]
    } else {
        console.warn(`Translation issue: "${id}" has no translation to language "${languageContext.language}"`)
        return id
    }
}

import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { Button, Divider } from '@equinor/eds-core-react'

const LanguageContainer = styled.div`
    display: flex;
    gap: 0rem;
`

const LanguageButton = styled(Button)<{ $language?: string; $text?: string }>`
    background: none;
    padding: 0;
    font: inherit;
    color: #6f6f6f;
    border-color: transparent;
    outline: none;
    font-weight: bold;
    ${(props) =>
        props.$text === props.$language
            ? `
        color: #007079;
        text-decoration: underline;
      &:hover {
          cursor: default;
          background: none;
        }`
            : `
         &:hover {
        cursor: pointer;
        background:#DEEDEE;
    }
      `}
`

const VerticalBar = styled(Divider)`
    width: 2px;
    height: 10px;
    margin: 13px 3px 1px 3px;
`

export function SelectLanguage() {
    const { language, switchLanguage } = useLanguageContext()
    const handleLanguageChange = (selectedLanguage: string) => {
        switchLanguage(selectedLanguage)
    }
    return (
        <LanguageContainer>
            <LanguageButton $text="en" $language={language} onClick={() => handleLanguageChange('en')}>
                EN
            </LanguageButton>
            <VerticalBar />
            <LanguageButton $text="no" $language={language} onClick={() => handleLanguageChange('no')}>
                NO
            </LanguageButton>
        </LanguageContainer>
    )
}

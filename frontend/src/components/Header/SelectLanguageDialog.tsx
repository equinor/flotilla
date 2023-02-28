import { LanguageShort, languageOptions } from 'language'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Button, Icon, Autocomplete, Dialog, Card } from '@equinor/eds-core-react'
import { chevron_down } from '@equinor/eds-icons'
import { useState } from 'react'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'

Icon.add({ chevron_down })

const StyledLanguageDialog = styled.div`
    display: flex;
    justify-content: center;
`

const StyledAutoComplete = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
`

const StyledLanguageSection = styled.div`
    margin-left: auto;
    margin-right: 0px;
`

export function SelectLanguageDialog() {
    const { language, switchLanguage } = useLanguageContext()
    const [newLanguage, setNewLanguage] = useState<string>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)

    return (
        <>
            <Button variant="ghost" onClick={() => setIsDialogOpen(true)}>
                {languageOptions[language]}
                <Icon name="chevron_down" size={16} title="user" />
            </Button>
            <StyledLanguageDialog>
                <Dialog open={isDialogOpen} isDismissable>
                    <StyledAutoComplete>
                        <Autocomplete
                            options={Array.from(Object.values(languageOptions)).sort()}
                            label={Text('Select language')}
                            initialSelectedOptions={[]}
                            onOptionsChange={({ selectedItems }) => {
                                selectedItems[0]
                                    ? setNewLanguage(LanguageShort(selectedItems[0]))
                                    : setNewLanguage(undefined)
                            }}
                        />
                        <StyledLanguageSection>
                            <Button
                                onClick={() => {
                                    setNewLanguage(undefined)
                                    setIsDialogOpen(false)
                                }}
                                variant="outlined"
                                color="secondary"
                            >
                                {' '}
                                {Text('Cancel')}{' '}
                            </Button>
                            <Button
                                onClick={() => {
                                    if (newLanguage) switchLanguage(newLanguage)
                                    setNewLanguage(undefined)
                                    setIsDialogOpen(false)
                                }}
                                disabled={!newLanguage}
                            >
                                {' '}
                                {Text('Save')}
                            </Button>
                        </StyledLanguageSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledLanguageDialog>
        </>
    )
}

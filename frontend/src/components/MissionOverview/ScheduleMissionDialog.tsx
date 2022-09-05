import { Autocomplete, AutocompleteChanges, Button, Card, Checkbox, Dialog, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useState } from 'react'
import styled from 'styled-components'

interface IProps {
    options: Array<string>
    onSelectedMissions: (missions: string[]) => void
    onScheduleButtonPress: () => void
}

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: center;
`
const StyledAutoComplete = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
`

const StyledMissionSection = styled.div`
    margin-left: auto;
    margin-right: 0;
`

export const ScheduleMissionDialog = (props: IProps): JSX.Element => {
    const [isOpen, setIsOpen] = useState<boolean>(false)
    const onChange = (changes: AutocompleteChanges<string>) => {
        props.onSelectedMissions(changes.selectedItems)
    }
    return (
        <>
            <Button
                onClick={() => {
                    setIsOpen(true)
                }}
            >
                Schedule Mission
            </Button>
            <StyledMissionDialog>
                <Dialog open={isOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h5">Schedule mission</Typography>
                        <Autocomplete
                            options={props.options}
                            label={'Schedule Missions'}
                            onOptionsChange={onChange}
                            multiple
                        />
                        <StyledMissionSection>
                            <Button
                                onClick={() => {
                                    setIsOpen(false)
                                }}
                                variant="outlined"
                                color="secondary"
                            >
                                {' '}
                                Cancel{' '}
                            </Button>
                            <Button
                                onClick={() => {
                                    props.onScheduleButtonPress()
                                    setIsOpen(false)
                                }}
                            >
                                {' '}
                                Schedule mission
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledMissionDialog>
        </>
    )
}

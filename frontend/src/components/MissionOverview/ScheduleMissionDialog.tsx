import { Autocomplete, AutocompleteChanges, Button, Card, Checkbox, Dialog, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useState } from 'react'
import styled from 'styled-components'

interface IProps {
    robotOptions: Array<string>
    echoMissionsOptions: Array<string>
    onSelectedMissions: (missions: string[]) => void
    onSelectedRobot: (robot: string) => void
    onScheduleButtonPress: () => void
    scheduleButtonDisabled: boolean
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
    const onChangeEchoMissionSelections = (changes: AutocompleteChanges<string>) => {
        props.onSelectedMissions(changes.selectedItems)
    }
    const onChangeRobotSelection = (changes: AutocompleteChanges<string>) => {
        props.onSelectedRobot(changes.selectedItems[0])
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
                            options={props.echoMissionsOptions}
                            label={'Schedule Missions'}
                            onOptionsChange={onChangeEchoMissionSelections}
                            multiple
                        />
                        <Autocomplete
                            options={props.robotOptions}
                            label={'Select robot'}
                            onOptionsChange={onChangeRobotSelection}
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
                                disabled={props.scheduleButtonDisabled}
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

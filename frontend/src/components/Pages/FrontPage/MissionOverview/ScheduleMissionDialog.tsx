import {
    Autocomplete,
    AutocompleteChanges,
    Button,
    Card,
    Checkbox,
    Dialog,
    Typography,
    TextField,
} from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { ChangeEvent, useState } from 'react'
import styled from 'styled-components'

interface IProps {
    robotOptions: Array<string>
    echoMissionsOptions: Array<string>
    onSelectedMissions: (missions: string[]) => void
    onSelectedRobot: (robot: string) => void
    onSelectedStartTime: (time: string) => void
    onScheduleButtonPress: () => void
    scheduleButtonDisabled: boolean
    frontPageScheduleButtonDisabled: boolean
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
    const [isStartTimeValid, setIsStartTimeValid] = useState<boolean>(true)
    const onChangeEchoMissionSelections = (changes: AutocompleteChanges<string>) => {
        props.onSelectedMissions(changes.selectedItems)
    }
    const onChangeRobotSelection = (changes: AutocompleteChanges<string>) => {
        props.onSelectedRobot(changes.selectedItems[0])
    }
    const onChangeStartTime = (changes: ChangeEvent<HTMLInputElement>) => {
        const allowedPastStartTime = 60 * 1000
        if (!(new Date(changes.target.value).getTime() < new Date().getTime() - allowedPastStartTime)) {
            setIsStartTimeValid(true)
            props.onSelectedStartTime(changes.target.value)
        } else {
            setIsStartTimeValid(false)
            props.onSelectedStartTime('')
        }
    }
    return (
        <>
            <Button
                onClick={() => {
                    setIsOpen(true)
                }}
                disabled={props.frontPageScheduleButtonDisabled}
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
                        <TextField
                            id="datetime"
                            label="Select start time"
                            type="datetime-local"
                            variant={isStartTimeValid ? undefined : 'error'}
                            helperText={isStartTimeValid ? undefined : 'Cannot schedule mission in the past'}
                            onChange={onChangeStartTime}
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

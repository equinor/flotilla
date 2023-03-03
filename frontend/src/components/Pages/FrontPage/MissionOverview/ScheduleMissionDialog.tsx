import {
    Autocomplete,
    AutocompleteChanges,
    Button,
    Card,
    Dialog,
    Typography,
    TextField,
    Popover,
} from '@equinor/eds-core-react'
import { ChangeEvent, useRef, useState } from 'react'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'

interface IProps {
    robotOptions: Array<string>
    echoMissionsOptions: Array<string>
    onSelectedMissions: (missions: string[]) => void
    onSelectedRobot: (robot: string) => void
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
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const [isAssetValid, setIsAssetValid] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)
    let timer: ReturnType<typeof setTimeout>
    const openPopover = () => {
        if (props.frontPageScheduleButtonDisabled) setIsPopoverOpen(true)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleHover = () => {
        timer = setTimeout(() => {
            openPopover()
        }, 300)

        if (sessionStorage.getItem('assetString')) setIsAssetValid(true)
        else setIsAssetValid(false)
    }

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    const onChangeEchoMissionSelections = (changes: AutocompleteChanges<string>) => {
        props.onSelectedMissions(changes.selectedItems)
    }
    const onChangeRobotSelection = (changes: AutocompleteChanges<string>) => {
        props.onSelectedRobot(changes.selectedItems[0])
    }
    return (
        <>
            <div onPointerEnter={handleHover} onPointerLeave={handleClose} onFocus={openPopover} onBlur={handleClose}>
                <Button
                    onClick={() => {
                        setIsDialogOpen(true)
                    }}
                    disabled={props.frontPageScheduleButtonDisabled}
                    ref={anchorRef}
                >
                    {Text('Schedule mission')}
                </Button>
            </div>

            <Popover
                anchorEl={anchorRef.current}
                onClose={handleClose}
                open={isPopoverOpen && !isAssetValid}
                placement="top"
            >
                <Popover.Content>
                    <Typography variant="body_short">{Text('Please select asset')}</Typography>
                </Popover.Content>
            </Popover>

            <Popover
                anchorEl={anchorRef.current}
                onClose={handleClose}
                open={isPopoverOpen && isAssetValid}
                placement="top"
            >
                <Popover.Content>
                    <Typography variant="body_short">
                        {Text('This asset has no missions - Please create mission')}
                    </Typography>
                </Popover.Content>
            </Popover>

            <StyledMissionDialog>
                <Dialog open={isDialogOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h5">{Text('Schedule mission')}</Typography>
                        <Autocomplete
                            options={props.echoMissionsOptions}
                            label={Text('Select missions')}
                            onOptionsChange={onChangeEchoMissionSelections}
                            multiple
                        />
                        <Autocomplete
                            options={props.robotOptions}
                            label={Text('Select robot')}
                            onOptionsChange={onChangeRobotSelection}
                        />
                        <StyledMissionSection>
                            <Button
                                onClick={() => {
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
                                    props.onScheduleButtonPress()
                                    setIsDialogOpen(false)
                                }}
                                disabled={props.scheduleButtonDisabled}
                            >
                                {' '}
                                {Text('Schedule mission')}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledMissionDialog>
        </>
    )
}

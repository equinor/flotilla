import {
    Autocomplete,
    AutocompleteChanges,
    Button,
    Card,
    Dialog,
    Typography,
    Popover,
    Icon,
} from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useRef, useState, useEffect } from 'react'
import { useAssetContext } from 'components/Contexts/AssetContext'

interface IProps {
    robotOptions: Array<string>
    echoMissionsOptions: Array<string>
    onSelectedMissions: (missions: string[]) => void
    onSelectedRobot: (robot: string) => void
    onScheduleButtonPress: () => void
    onFrontPageScheduleButtonPress: () => void
    scheduleButtonDisabled: boolean
    frontPageScheduleButtonDisabled: boolean
    createMissionButton: JSX.Element
    isLoadingEchoMissions: boolean
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
    const [isNoEchoMissionsDialogOpen, setIsNoEchoMissionsDialogOpen] = useState<boolean>(false)
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const [isScheduleMissionsPressed, setIsScheduleMissionsPressed] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)
    const { asset } = useAssetContext()

    useEffect(() => {
        if (!props.isLoadingEchoMissions && isScheduleMissionsPressed) {
            if (props.echoMissionsOptions.length === 0) setIsNoEchoMissionsDialogOpen(true)
            else setIsDialogOpen(true)
            setIsScheduleMissionsPressed(false)
        }
    }, [props.isLoadingEchoMissions])

    let timer: ReturnType<typeof setTimeout>
    const openPopover = () => {
        if (props.frontPageScheduleButtonDisabled) setIsPopoverOpen(true)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleHover = () => {
        timer = setTimeout(() => {
            openPopover()
        }, 300)
    }

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    const onClickScheduleMission = () => {
        setIsScheduleMissionsPressed(true)
        props.onFrontPageScheduleButtonPress()
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
                        onClickScheduleMission()
                    }}
                    disabled={props.frontPageScheduleButtonDisabled}
                    ref={anchorRef}
                >
                    {!props.isLoadingEchoMissions && (
                        <>
                            <Icon name={Icons.Add} size={16} />
                            {Text('Add mission')}
                        </>
                    )}
                    {props.isLoadingEchoMissions && (
                        <>
                            <Icon name={Icons.Wait} size={16} />
                            {Text('Loading')}..
                        </>
                    )}
                </Button>
            </div>

            <Popover
                anchorEl={anchorRef.current}
                onClose={handleClose}
                open={isPopoverOpen && asset === ''}
                placement="top"
            >
                <Popover.Content>
                    <Typography variant="body_short">{Text('Please select asset')}</Typography>
                </Popover.Content>
            </Popover>

            <StyledMissionDialog>
                <Dialog open={isNoEchoMissionsDialogOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h5">
                            {Text('This asset has no missions - Please create mission')}
                        </Typography>
                        <StyledMissionSection>
                            {props.createMissionButton}
                            <Button
                                onClick={() => {
                                    setIsNoEchoMissionsDialogOpen(false)
                                }}
                                variant="outlined"
                                color="secondary"
                            >
                                {' '}
                                {Text('Cancel')}{' '}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledMissionDialog>

            <StyledMissionDialog>
                <Dialog open={isDialogOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h5">{Text('Add mission')}</Typography>
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
                                {Text('Add mission')}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledMissionDialog>
        </>
    )
}

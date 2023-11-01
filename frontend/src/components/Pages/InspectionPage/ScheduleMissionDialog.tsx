import { Autocomplete, Button, Dialog, Typography, Popover, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRef, useState, useEffect } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Robot } from 'models/Robot'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Icons } from 'utils/icons'

interface IProps {
    missions: CondensedMissionDefinition[]
    refreshInterval: number
    closeDialog: () => void
    setMissions: (selectedMissions: CondensedMissionDefinition[] | undefined) => void
    unscheduledMissions: CondensedMissionDefinition[]
    isAlreadyScheduled: boolean
}

interface IScheduledProps {
    openDialog: () => void
    closeDialog: () => void
}

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: space-between;
`
const StyledAutoComplete = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    padding: 8px;
    gap: 25px;
`
const StyledMissionSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`
const StyledDialog = styled(Dialog)`
    display: flex;
    flex-direction: column;
    padding: 1rem;
    width: 400px;
`

const StyledDialogContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 2px;
`
const StyledDangerContent = styled.div`
    display: flex;
    flex-direction: row;
    gap: 2px;
`
export const ScheduleMissionDialog = (props: IProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [robotOptions, setRobotOptions] = useState<Robot[]>([])
    const anchorRef = useRef<HTMLButtonElement>(null)

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getEnabledRobots()
                .then((robots) =>
                    robots.filter(
                        (robots) => robots.currentInstallation.toLowerCase() === installationCode.toLowerCase()
                    )
                )
                .then((robots) => {
                    setRobotOptions(robots)
                })
        }, props.refreshInterval)
        return () => clearInterval(id)
    }, [props.refreshInterval])

    let timer: ReturnType<typeof setTimeout>

    const onSelectedRobot = (selectedRobot: Robot) => {
        if (robotOptions === undefined) return

        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (selectedRobot === undefined) return

        props.missions.forEach((mission) => BackendAPICaller.scheduleMissionDefinition(mission.id, selectedRobot.id))

        setSelectedRobot(undefined)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    const onScheduleOnlyButtonPress = () => {
        if (selectedRobot === undefined) return

        props.unscheduledMissions.forEach((mission) =>
            BackendAPICaller.scheduleMissionDefinition(mission.id, selectedRobot.id)
        )

        setSelectedRobot(undefined)
    }

    return (
        <>
            <Popover
                anchorEl={anchorRef.current}
                onClose={handleClose}
                open={isPopoverOpen && installationCode === ''}
                placement="top"
            >
                <Popover.Content>
                    <Typography variant="body_short">{TranslateText('Please select installation')}</Typography>
                </Popover.Content>
            </Popover>

            <StyledMissionDialog>
                <StyledDialog open={true}>
                    <StyledDialogContent>
                        <Typography variant="h4">{TranslateText('Add mission')}</Typography>
                        {props.isAlreadyScheduled && (
                            <StyledDangerContent>
                                <Icon name={Icons.Warning} size={16} color="red" />
                                <Typography variant="body_short" color="red">
                                    {props.missions.length > 1
                                        ? TranslateText('Some missions are already in the queue')
                                        : TranslateText('The mission is already in the queue')}
                                </Typography>
                            </StyledDangerContent>
                        )}
                        <StyledAutoComplete>
                            <Autocomplete
                                optionLabel={(r) => r.name + ' (' + r.model.type + ')'}
                                options={robotOptions}
                                label={TranslateText('Select robot')}
                                onOptionsChange={(changes) => onSelectedRobot(changes.selectedItems[0])}
                                autoWidth={true}
                                onFocus={(e) => e.preventDefault()}
                            />

                            <StyledMissionSection>
                                <Button
                                    onClick={() => {
                                        props.closeDialog()
                                    }}
                                    variant="outlined"
                                    color="primary"
                                >
                                    {' '}
                                    {TranslateText('Cancel')}{' '}
                                </Button>
                                <Button
                                    onClick={() => {
                                        onScheduleButtonPress()
                                        props.closeDialog()
                                    }}
                                    disabled={!selectedRobot}
                                >
                                    {' '}
                                    {props.missions.length > 1
                                        ? TranslateText('Queue all missions')
                                        : TranslateText('Queue mission')}
                                </Button>
                                {props.isAlreadyScheduled && props.unscheduledMissions.length > 0 && (
                                    <Button
                                        onClick={() => {
                                            onScheduleOnlyButtonPress()
                                            props.closeDialog()
                                        }}
                                        disabled={!selectedRobot}
                                    >
                                        {' '}
                                        {TranslateText('Queue unscheduled missions')}
                                    </Button>
                                )}
                            </StyledMissionSection>
                        </StyledAutoComplete>
                    </StyledDialogContent>
                </StyledDialog>
            </StyledMissionDialog>
        </>
    )
}

export const AlreadyScheduledMissionDialog = (props: IScheduledProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <StyledDialog open={true}>
                <Dialog.Header>{TranslateText('The mission is already in the queue')}</Dialog.Header>
                <Dialog.CustomContent>
                    <Typography variant="body_short">{TranslateText('AlreadyScheduledText')}</Typography>
                </Dialog.CustomContent>
                <Dialog.Actions>
                    <StyledMissionSection>
                        <Button
                            onClick={() => {
                                props.closeDialog()
                            }}
                            variant="outlined"
                            color="primary"
                        >
                            {' '}
                            {TranslateText('Cancel')}{' '}
                        </Button>
                        <Button
                            onClick={() => {
                                props.openDialog()
                                props.closeDialog()
                            }}
                        >
                            {' '}
                            {TranslateText('Queue mission')}
                        </Button>
                    </StyledMissionSection>
                </Dialog.Actions>
            </StyledDialog>
        </>
    )
}

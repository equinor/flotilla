import { Autocomplete, Button, Dialog, Typography, Popover, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRef, useState, useEffect } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Robot } from 'models/Robot'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Icons } from 'utils/icons'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { StyledAutoComplete, StyledDialog } from 'components/Styles/StyledComponents'

interface IProps {
    missions: CondensedMissionDefinition[]
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
const StyledMissionSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
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
    const { enabledRobots } = useRobotContext()
    const anchorRef = useRef<HTMLButtonElement>(null)

    let timer: ReturnType<typeof setTimeout>

    useEffect(() => {
        const relevantRobots = [...enabledRobots].filter(
            (robot) => robot.currentInstallation.installationCode.toLowerCase() === installationCode.toLowerCase()
        )
        setRobotOptions(relevantRobots)
    }, [enabledRobots, installationCode])

    const onSelectedRobot = (selectedRobot: Robot) => {
        if (!robotOptions) return

        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (!selectedRobot) return

        props.missions.forEach((mission) => BackendAPICaller.scheduleMissionDefinition(mission.id, selectedRobot.id))

        setSelectedRobot(undefined)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    const onScheduleOnlyButtonPress = () => {
        if (!selectedRobot) return

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

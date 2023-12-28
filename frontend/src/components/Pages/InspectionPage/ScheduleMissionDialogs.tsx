import { Autocomplete, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Robot } from 'models/Robot'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Icons } from 'utils/icons'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { StyledAutoComplete, StyledButton, StyledDialog } from 'components/Styles/StyledComponents'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { ScheduleMissionWithLocalizationVerificationDialog } from 'components/Displays/LocalizationVerification/ScheduleMissionWithLocalizationVerification'

interface IProps {
    selectedMissions: CondensedMissionDefinition[]
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
    gap: 6px;
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
    const { enabledRobots } = useRobotContext()
    const { installationCode } = useInstallationContext()
    const { setLoadingMissionSet } = useMissionsContext()
    const { setAlert } = useAlertContext()
    const [isLocalizationVerificationDialogOpen, setIsLocalizationVerificationDialog] = useState<boolean>(false)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [missionsToSchedule, setMissionsToSchedule] = useState<CondensedMissionDefinition[]>()
    const [robotOptions, setRobotOptions] = useState<Robot[]>([])

    useEffect(() => {
        const relevantRobots = [...enabledRobots].filter(
            (robot) => robot.currentInstallation.installationCode.toLowerCase() === installationCode.toLowerCase()
        )
        setRobotOptions(relevantRobots)
    }, [enabledRobots, installationCode])

    const onSelectedRobot = (selectedRobot: Robot) => {
        if (robotOptions) setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = (missions: CondensedMissionDefinition[]) => () => {
        if (!selectedRobot) return

        setMissionsToSchedule(missions)
        setIsLocalizationVerificationDialog(true)
    }

    const scheduleMissions = () => {
        setIsLocalizationVerificationDialog(false)

        if (!selectedRobot || !missionsToSchedule) return

        missionsToSchedule.forEach((mission) => {
            BackendAPICaller.scheduleMissionDefinition(mission.id, selectedRobot.id).catch((e) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent message={TranslateText('Failed to schedule mission ') + `${mission.name}. ${e.message}`} />
                )
                setLoadingMissionSet((currentSet: Set<string>) => {
                    const updatedSet: Set<string> = new Set(currentSet)
                    updatedSet.delete(String(mission.name))
                    return updatedSet
                })
            })
            setLoadingMissionSet((currentSet: Set<string>) => {
                const updatedSet: Set<string> = new Set(currentSet)
                updatedSet.add(String(mission.name))
                return updatedSet
            })
        })

        setMissionsToSchedule(undefined)
        setSelectedRobot(undefined)
        props.closeDialog()
    }

    const closeScheduleDialogs = () => {
        setIsLocalizationVerificationDialog(false)
        props.closeDialog()
    }

    return (
        <>
            <StyledMissionDialog>
                <StyledDialog open={!isLocalizationVerificationDialogOpen}>
                    <StyledDialogContent>
                        <Typography variant="h4">{TranslateText('Add mission to the queue')}</Typography>
                        {props.isAlreadyScheduled && (
                            <StyledDangerContent>
                                <Icon name={Icons.Warning} size={16} color="red" />
                                <Typography variant="body_short" color="red">
                                    {props.selectedMissions.length > 1
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
                                <StyledButton
                                    onClick={() => {
                                        props.closeDialog()
                                    }}
                                    variant="outlined"
                                >
                                    {TranslateText('Cancel')}
                                </StyledButton>
                                <StyledButton
                                    onClick={onScheduleButtonPress(props.selectedMissions)}
                                    disabled={!selectedRobot}
                                >
                                    {' '}
                                    {props.selectedMissions.length > 1
                                        ? TranslateText('Queue all missions')
                                        : TranslateText('Queue mission')}
                                </StyledButton>
                                {props.isAlreadyScheduled && props.unscheduledMissions.length > 0 && (
                                    <StyledButton
                                        onClick={onScheduleButtonPress(props.unscheduledMissions)}
                                        disabled={!selectedRobot}
                                    >
                                        {TranslateText('Queue unscheduled missions')}
                                    </StyledButton>
                                )}
                            </StyledMissionSection>
                        </StyledAutoComplete>
                    </StyledDialogContent>
                </StyledDialog>
            </StyledMissionDialog>
            {isLocalizationVerificationDialogOpen && (
                <ScheduleMissionWithLocalizationVerificationDialog
                    scheduleMissions={scheduleMissions}
                    closeDialog={closeScheduleDialogs}
                    robotId={selectedRobot!.id}
                    missionDeckNames={props.selectedMissions.map((mission) => mission.area?.deckName ?? '')}
                />
            )}
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
                        <StyledButton
                            onClick={() => {
                                props.closeDialog()
                            }}
                            variant="outlined"
                        >
                            {' '}
                            {TranslateText('Cancel')}{' '}
                        </StyledButton>
                        <StyledButton
                            onClick={() => {
                                props.openDialog()
                                props.closeDialog()
                            }}
                        >
                            {' '}
                            {TranslateText('Queue mission')}
                        </StyledButton>
                    </StyledMissionSection>
                </Dialog.Actions>
            </StyledDialog>
        </>
    )
}

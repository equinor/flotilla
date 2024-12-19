import { Autocomplete, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState } from 'react'
import { Robot, RobotStatus } from 'models/Robot'
import { MissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Icons } from 'utils/icons'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { StyledAutoComplete, StyledButton, StyledDialog } from 'components/Styles/StyledComponents'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { ScheduleMissionWithConfirmDialogs } from 'components/Displays/ConfirmScheduleDialogs/ConfirmScheduleDialog'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

interface IProps {
    selectedMissions: MissionDefinition[]
    closeDialog: () => void
    setMissions: (selectedMissions: MissionDefinition[] | undefined) => void
    unscheduledMissions: MissionDefinition[]
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
    const { setLoadingRobotMissionSet } = useMissionsContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isLocalizationVerificationDialogOpen, setIsLocalizationVerificationDialog] = useState<boolean>(false)
    const [missionsToSchedule, setMissionsToSchedule] = useState<MissionDefinition[]>()
    const filteredRobots = enabledRobots.filter(
        (r) =>
            (r.status === RobotStatus.Available ||
                r.status === RobotStatus.Busy ||
                r.status === RobotStatus.Recharging) &&
            r.isarConnected
    )
    const [selectedRobot, setSelectedRobot] = useState<Robot | undefined>(
        filteredRobots.length === 1 ? filteredRobots[0] : undefined
    )

    const onSelectedRobot = (selectedRobot: Robot) => {
        if (filteredRobots) setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = (missions: MissionDefinition[]) => () => {
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
                    <FailedRequestAlertContent
                        translatedMessage={
                            TranslateText('Failed to schedule mission') + ` '${mission.name}'. ${e.message}`
                        }
                    />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent
                        translatedMessage={
                            TranslateText('Failed to schedule mission') + ` '${mission.name}'. ${e.message}`
                        }
                    />,
                    AlertCategory.ERROR
                )
                setLoadingRobotMissionSet((currentSet: Set<string>) => {
                    const updatedSet: Set<string> = new Set(currentSet)
                    updatedSet.delete(String(mission.name + selectedRobot.id))
                    return updatedSet
                })
            })
            setLoadingRobotMissionSet((currentSet: Set<string>) => {
                const updatedSet: Set<string> = new Set(currentSet)
                updatedSet.add(String(mission.name + selectedRobot.id))
                return updatedSet
            })
        })

        if (missionsToSchedule.length > 0) {
            let robotCard = document.getElementById(FrontPageSectionId.RobotCard + selectedRobot.id)
            let topBarHeight = document.getElementById(FrontPageSectionId.TopBar)?.offsetHeight ?? 0

            if (robotCard) {
                window.scroll({ top: robotCard.offsetTop - topBarHeight, behavior: 'smooth' })
            }
        }

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
                                initialSelectedOptions={selectedRobot ? [selectedRobot] : []}
                                dropdownHeight={200}
                                optionLabel={(r) => r.name + ' (' + r.model.type + ')'}
                                options={filteredRobots}
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
                <ScheduleMissionWithConfirmDialogs
                    scheduleMissions={scheduleMissions}
                    closeDialog={closeScheduleDialogs}
                    robotId={selectedRobot!.id}
                    missionInspectionAreaNames={props.selectedMissions.map(
                        (mission) => mission.inspectionArea?.inspectionAreaName ?? ''
                    )}
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

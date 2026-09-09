import { Autocomplete, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState } from 'react'
import { RobotWithoutTelemetry, RobotStatus } from 'models/Robot'
import { MissionDefinition } from 'models/MissionDefinition'
import { Icons } from 'utils/icons'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { StyledAutoComplete, StyledButton, StyledDialog } from 'components/Styles/StyledComponents'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertType, AlertKind } from 'models/Alert'
import { InspectionAreaVerificationDialog } from 'components/Displays/InspectionAreaVerificationDialogs/InspectionAreaVerificationDialog'
import {
    getInspectionAreaDialogType,
    InspectionAreaDialogType,
} from 'components/Displays/InspectionAreaVerificationDialogs/getInspectionAreaDialogType'
import { phone_width } from 'utils/constants'
import { useBackendApi } from 'api/UseBackendApi'

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: space-between;
    width: 580px;

    @media (max-width: ${phone_width}) {
        width: 80vw;
    }
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

interface IProps {
    selectedMissions: MissionDefinition[]
    closeDialog: () => void
    unscheduledMissions: MissionDefinition[]
    isAlreadyScheduled: boolean
}

export const ScheduleMissionDialog = (props: IProps) => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { setLoadingRobotMissionSet } = useMissionsContext()
    const { raiseAlert } = useAlertContext()
    const [isInspectionAreaVerificationDialogOpen, setIsInspectionAreaVerificationDialogOpen] = useState<boolean>(false)
    const [verificationDialogType, setVerificationDialogType] = useState<InspectionAreaDialogType | null>(null)
    const [missionsToSchedule, setMissionsToSchedule] = useState<MissionDefinition[]>()
    const backendApi = useBackendApi()
    const filteredRobots = enabledRobots.filter(
        (r) =>
            r.status === RobotStatus.Available ||
            r.status === RobotStatus.Home ||
            r.status === RobotStatus.ReturningHome ||
            r.status === RobotStatus.Busy ||
            r.status === RobotStatus.Recharging
    )
    const [selectedRobot, setSelectedRobot] = useState<RobotWithoutTelemetry | undefined>(
        filteredRobots.length === 1 ? filteredRobots[0] : undefined
    )

    const onSelectedRobot = (selectedRobot: RobotWithoutTelemetry) => {
        if (filteredRobots) setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = (missions: MissionDefinition[]) => () => {
        if (!selectedRobot) return

        const dialogType = getInspectionAreaDialogType(
            selectedRobot,
            missions.map((mission) => mission.inspectionArea)
        )

        if (dialogType === null) {
            scheduleMissions(missions)
            return
        }

        setMissionsToSchedule(missions)
        setVerificationDialogType(dialogType)
        setIsInspectionAreaVerificationDialogOpen(true)
    }

    const scheduleMissions = (missions: MissionDefinition[]) => {
        setIsInspectionAreaVerificationDialogOpen(false)

        if (!selectedRobot) return

        missions.forEach((mission) => {
            backendApi.scheduleMissionDefinition(mission.id, selectedRobot.id).catch((e) => {
                raiseAlert(AlertType.RequestFail, {
                    kind: AlertKind.RequestFail,
                    message: TranslateText('Failed to schedule mission') + ` '${mission.name}'. ${e.message}`,
                })
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

        setMissionsToSchedule(undefined)
        setSelectedRobot(undefined)
        props.closeDialog()
    }

    const closeScheduleDialogs = () => {
        setIsInspectionAreaVerificationDialogOpen(false)
        props.closeDialog()
    }

    return (
        <>
            <StyledMissionDialog>
                <StyledDialog open={!isInspectionAreaVerificationDialogOpen}>
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
                                optionLabel={(r) => r.name + ' (' + r.type + ')'}
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
            {isInspectionAreaVerificationDialogOpen && verificationDialogType !== null && (
                <InspectionAreaVerificationDialog
                    dialogType={verificationDialogType}
                    closeDialog={closeScheduleDialogs}
                    robot={selectedRobot}
                    missionInspectionAreas={missionsToSchedule?.map((mission) => mission.inspectionArea) ?? []}
                />
            )}
        </>
    )
}

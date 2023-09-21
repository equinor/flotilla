import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, placeholderMission } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Robot } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { CreateMissionButton } from './CreateMissionButton'
import { EchoMissionDefinition } from 'models/MissionDefinition'
import { useMissionsContext } from 'components/Contexts/MissionListsContext'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 1rem;
`

const MissionButtonView = styled.div`
    display: flex;
    gap: 1rem;
`
const mapEchoMissionToString = (missions: EchoMissionDefinition[]): Map<string, EchoMissionDefinition> => {
    var missionMap = new Map<string, EchoMissionDefinition>()
    missions.forEach((mission: EchoMissionDefinition) => {
        missionMap.set(mission.echoMissionId + ': ' + mission.name, mission)
    })
    return missionMap
}

export function MissionQueueView() {
    const { TranslateText } = useLanguageContext()
    const { missionQueue, ongoingMissions } = useMissionsContext()
    const { installationCode } = useInstallationContext()
    const { setAlert } = useAlertContext()

    const [loadingMissionNames, setLoadingMissionNames] = useState<Set<string>>(new Set())
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMissionDefinition[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMissionDefinition>>(
        new Map<string, EchoMissionDefinition>()
    )
    const { enabledRobots } = useRobotContext()
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)

    const fetchEchoMissions = () => {
        setIsFetchingEchoMissions(true)
        BackendAPICaller.getAvailableEchoMissions(installationCode as string)
            .then((missions) => {
                const echoMissionsMap: Map<string, EchoMissionDefinition> = mapEchoMissionToString(missions)
                setEchoMissions(echoMissionsMap)
                setIsFetchingEchoMissions(false)
            })
            .catch((_) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent message={'Failed to retrieve echo missions'} />
                )
                setIsFetchingEchoMissions(false)
            })
    }

    const onChangeMissionSelections = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: EchoMissionDefinition[] = []
        if (echoMissions) {
            selectedEchoMissions.forEach((selectedEchoMission: string) => {
                echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as EchoMissionDefinition)
            })
        }
        setSelectedEchoMissions(echoMissionsToSchedule)
    }
    const onSelectedRobot = (selectedRobot: Robot) => {
        if (!enabledRobots) return
        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (!selectedRobot) return

        selectedEchoMissions.forEach((mission: EchoMissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, installationCode)
            setLoadingMissionNames((prev) => {
                const updatedSet = new Set(prev)
                updatedSet.add(mission.name)
                return updatedSet
            })
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        BackendAPICaller.deleteMission(mission.id)
    }

    useEffect(() => {
        if (!selectedRobot || selectedEchoMissions.length === 0) {
            setScheduleButtonDisabled(true)
        } else {
            setScheduleButtonDisabled(false)
        }
    }, [selectedRobot, selectedEchoMissions])

    useEffect(() => {
        if (enabledRobots.length === 0 || installationCode === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [enabledRobots, installationCode])

    useEffect(() => {
        setLoadingMissionNames((currentLoadingNames) => {
            const updatedLoadingMissionIds = new Set(currentLoadingNames)
            missionQueue.forEach((mission) => updatedLoadingMissionIds.delete(mission.name))
            ongoingMissions.forEach((mission) => updatedLoadingMissionIds.delete(mission.name))
            return updatedLoadingMissionIds
        })
    }, [missionQueue, ongoingMissions])

    const missionQueueDisplay = missionQueue.map((mission, index) => (
        <MissionQueueCard key={index} order={index + 1} mission={mission} onDeleteMission={onDeleteMission} />
    ))

    const loadingQueueDisplay = (
        <MissionQueueCard
            key={'placeholder'}
            order={missionQueue.length + 1}
            mission={placeholderMission}
            onDeleteMission={() => {}}
        />
    )

    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Mission Queue')}
            </Typography>
            <MissionTable>
                {missionQueue.length > 0 && missionQueueDisplay}
                {loadingMissionNames.size > 0 && loadingQueueDisplay}
                {loadingMissionNames.size === 0 && missionQueue.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog
                    robotOptions={enabledRobots}
                    echoMissionsOptions={Array.from(echoMissions.keys())}
                    onChangeMissionSelections={onChangeMissionSelections}
                    selectedMissions={selectedEchoMissions}
                    onSelectedRobot={onSelectedRobot}
                    onScheduleButtonPress={onScheduleButtonPress}
                    fetchEchoMissions={fetchEchoMissions}
                    scheduleButtonDisabled={scheduleButtonDisabled}
                    frontPageScheduleButtonDisabled={frontPageScheduleButtonDisabled}
                    isFetchingEchoMissions={isFetchingEchoMissions}
                ></ScheduleMissionDialog>
                {CreateMissionButton()}
            </MissionButtonView>
        </StyledMissionView>
    )
}

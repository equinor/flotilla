import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Robot } from 'models/Robot'
import { RefreshProps } from '../FrontPage'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { CreateMissionButton } from './CreateMissionButton'
import { MissionDefinition } from 'models/MissionDefinition'
import { useMissionQueueContext } from 'components/Contexts/MissionQueueContext'

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
const mapEchoMissionToString = (missions: MissionDefinition[]): Map<string, MissionDefinition> => {
    var missionMap = new Map<string, MissionDefinition>()
    missions.forEach((mission: MissionDefinition) => {
        missionMap.set(mission.echoMissionId + ': ' + mission.name, mission)
    })
    return missionMap
}

export function MissionQueueView({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { missionQueue } = useMissionQueueContext()

    const missionPageSize = 100
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<MissionDefinition[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, MissionDefinition>>(
        new Map<string, MissionDefinition>()
    )
    const [robotOptions, setRobotOptions] = useState<Robot[]>([])
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)
    const { installationCode } = useInstallationContext()

    const fetchEchoMissions = () => {
        setIsFetchingEchoMissions(true)
        BackendAPICaller.getAvailableEchoMission(installationCode as string).then((missions) => {
            const echoMissionsMap: Map<string, MissionDefinition> = mapEchoMissionToString(missions)
            setEchoMissions(echoMissionsMap)
            setIsFetchingEchoMissions(false)
        })
    }

    const onChangeMissionSelections = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: MissionDefinition[] = []
        if (echoMissions) {
            selectedEchoMissions.forEach((selectedEchoMission: string) => {
                echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as MissionDefinition)
            })
        }
        setSelectedEchoMissions(echoMissionsToSchedule)
    }
    const onSelectedRobot = (selectedRobot: Robot) => {
        if (robotOptions === undefined) return

        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (selectedRobot === undefined) return

        selectedEchoMissions.forEach((mission: MissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, installationCode)
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        BackendAPICaller.deleteMission(mission.id) //.catch((e) => handleError(e))
    }

    const onReorderMission = (missionIndex1: number, missionIndex2: number) => {
        if (
            missionIndex1 >= missionQueue.length ||
            missionIndex1 < 0 ||
            missionIndex2 >= missionQueue.length ||
            missionIndex2 < 0
        )
            return
        BackendAPICaller.updateMissionOrder(
            {
                status: MissionStatus.Pending,
                pageSize: missionPageSize,
            },
            missionQueue[missionIndex1].id,
            missionQueue[missionIndex2].id
        )
    }

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getEnabledRobots().then((robots) => {
                setRobotOptions(robots)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getMissionRuns({
                status: MissionStatus.Pending,
                pageSize: missionPageSize,
                orderBy: 'DesiredStartTime',
            }).then((missions) => {
                setMissionQueue(missions.content)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    useEffect(() => {
        if (selectedRobot === undefined || selectedEchoMissions.length === 0) {
            setScheduleButtonDisabled(true)
        } else {
            setScheduleButtonDisabled(false)
        }
    }, [selectedRobot, selectedEchoMissions])

    useEffect(() => {
        if (robotOptions.length === 0 || installationCode === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [robotOptions, installationCode])

    var missionQueueDisplay = missionQueue.map((mission, index) => (
        <MissionQueueCard
            key={mission.id}
            order={index}
            mission={mission}
            onDeleteMission={onDeleteMission}
            onReorderMission={onReorderMission}
        />
    ))

    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Mission Queue')}
            </Typography>
            <MissionTable>
                {missionQueue.length > 0 && missionQueueDisplay}
                {missionQueue.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog
                    robotOptions={robotOptions}
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

import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { EchoMission } from 'models/EchoMission'
import { Robot } from 'models/Robot'
import { RefreshProps } from '../FrontPage'
import { Text } from 'components/Contexts/LanguageContext'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { useErrorHandler } from 'react-error-boundary'
import { CreateMissionButton } from './CreateMissionButton'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    gap: 1rem;
`

const MissionButtonView = styled.div`
    display: flex;
    gap: 1rem;
`
const mapEchoMissionToString = (missions: EchoMission[]): Map<string, EchoMission> => {
    var missionMap = new Map<string, EchoMission>()
    missions.map((mission: EchoMission) => {
        missionMap.set(mission.id + ': ' + mission.name, mission)
    })
    return missionMap
}

const mapRobotsToString = (robots: Robot[]): Map<string, Robot> => {
    var robotMap = new Map<string, Robot>()
    robots.map((robot: Robot) => {
        robotMap.set(robot.name + ' (' + robot.model.type + ')', robot)
    })
    return robotMap
}

export function MissionQueueView({ refreshInterval }: RefreshProps) {
    const missionPageSize = 100
    const handleError = useErrorHandler()
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMission[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMission>>(new Map<string, EchoMission>())
    const [robotOptions, setRobotOptions] = useState<Map<string, Robot>>(new Map<string, Robot>())
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)
    const { assetCode } = useAssetContext()

    const fetchEchoMissions = () => {
        setIsFetchingEchoMissions(true)
        BackendAPICaller.getEchoMissions(assetCode as string).then((missions) => {
            const echoMissionsMap: Map<string, EchoMission> = mapEchoMissionToString(missions)
            setEchoMissions(echoMissionsMap)
            setIsFetchingEchoMissions(false)
        })
        //.catch((e) => handleError(e))
    }

    const onSelectedEchoMissions = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: EchoMission[] = []
        selectedEchoMissions.map((selectedEchoMission: string) => {
            if (echoMissions) echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as EchoMission)
        })
        setSelectedEchoMissions(echoMissionsToSchedule)
    }
    const onSelectedRobot = (selectedRobot: string) => {
        if (robotOptions === undefined) return

        setSelectedRobot(robotOptions.get(selectedRobot) as Robot)
    }

    const onScheduleButtonPress = () => {
        if (selectedRobot === undefined) return

        selectedEchoMissions.map((mission: EchoMission) => {
            BackendAPICaller.postMission(mission.id, selectedRobot.id, assetCode) //.catch((e) => handleError(e))
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        BackendAPICaller.deleteMission(mission.id) //.catch((e) => handleError(e))
    }

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getEnabledRobots().then((robots) => {
                const mappedRobots: Map<string, Robot> = mapRobotsToString(robots)
                setRobotOptions(mappedRobots)
            })
            //.catch((e) => handleError(e))
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getMissions({
                status: MissionStatus.Pending,
                pageSize: missionPageSize,
                orderBy: 'DesiredStartTime desc',
            }).then((missions) => {
                setMissionQueue(missions.content)
            })
            //.catch((e) => handleError(e))
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    useEffect(() => {
        if (selectedRobot === undefined || selectedEchoMissions.length === 0) {
            setScheduleButtonDisabled(true)
        } else {
            setScheduleButtonDisabled(false)
        }
    }, [selectedRobot, selectedEchoMissions])

    useEffect(() => {
        if (Array.from(robotOptions.keys()).length === 0 || assetCode === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [robotOptions, assetCode])

    var missionQueueDisplay = missionQueue.map(function (mission, index) {
        return <MissionQueueCard key={index} mission={mission} onDeleteMission={onDeleteMission} />
    })

    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {Text('Mission Queue')}
            </Typography>
            <MissionTable>
                {missionQueue.length > 0 && missionQueueDisplay}
                {missionQueue.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog
                    robotOptions={Array.from(robotOptions.keys())}
                    echoMissionsOptions={Array.from(echoMissions.keys())}
                    onSelectedMissions={onSelectedEchoMissions}
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

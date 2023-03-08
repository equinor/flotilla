import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { useApi } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { EchoMission } from 'models/EchoMission'
import { Robot } from 'models/Robot'
import { RefreshProps } from '../FrontPage'
import { Text } from 'components/Contexts/LanguageContext'
import { useAssetContext } from 'components/Contexts/AssetContext'

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
    gap: 2rem;
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
        robotMap.set(robot.name + ' id: ' + robot.id, robot)
    })
    return robotMap
}

export function MissionQueueView({ refreshInterval }: RefreshProps) {
    const apiCaller = useApi()
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMission[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMission>>(new Map<string, EchoMission>())
    const [robotOptions, setRobotOptions] = useState<Map<string, Robot>>(new Map<string, Robot>())
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [isLoadingEchoMissions, setIsLoadingEchoMissions] = useState<boolean>(false)
    const { asset } = useAssetContext()
    const echoURL = 'https://echo.equinor.com/mp?instCode='

    const onFrontPageScheduleButtonPress = () => {
        setIsLoadingEchoMissions(true)
        apiCaller.getEchoMissions(asset as string).then((missions) => {
            const mappedEchoMissions: Map<string, EchoMission> = mapEchoMissionToString(missions)
            setEchoMissions(mappedEchoMissions)
            setIsLoadingEchoMissions(false)
        })
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
            apiCaller.postMission(mission.id, selectedRobot.id, asset)
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        apiCaller.deleteMission(mission.id)
    }

    useEffect(() => {
        apiCaller.getMissionsByStatus(MissionStatus.Pending).then((missions) => {
            setMissionQueue(missions)
        })
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            apiCaller.getRobots().then((robots) => {
                const mappedRobots: Map<string, Robot> = mapRobotsToString(robots)
                setRobotOptions(mappedRobots)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            apiCaller.getMissionsByStatus(MissionStatus.Pending).then((missions) => {
                setMissionQueue(missions)
            })
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
        if (Array.from(robotOptions.keys()).length === 0 || asset === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [robotOptions, asset])

    var missionQueueDisplay = missionQueue.map(function (mission, index) {
        return <MissionQueueCard key={index} mission={mission} onDeleteMission={onDeleteMission} />
    })

    const createMissionButton = (
        <Button
            onClick={() => {
                window.open(echoURL + asset)
            }}
        >
            {Text('Create mission')}
        </Button>
    )

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
                    onFrontPageScheduleButtonPress={onFrontPageScheduleButtonPress}
                    scheduleButtonDisabled={scheduleButtonDisabled}
                    frontPageScheduleButtonDisabled={frontPageScheduleButtonDisabled}
                    isLoadingEchoMissions={isLoadingEchoMissions}
                    createMissionButton={createMissionButton}
                ></ScheduleMissionDialog>
                {createMissionButton}
            </MissionButtonView>
        </StyledMissionView>
    )
}

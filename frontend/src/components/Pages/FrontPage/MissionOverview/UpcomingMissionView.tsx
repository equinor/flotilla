import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { UpcomingMissionCard } from './UpcomingMissionCard'
import { useApi } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { NoUpcomingMissionsPlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { EchoMission } from 'models/EchoMission'
import { Robot } from 'models/Robot'
import { RefreshProps } from '../FrontPage'
import { Text } from 'components/Contexts/LanguageContext'

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

export function UpcomingMissionView({ refreshInterval }: RefreshProps) {
    const apiCaller = useApi()
    const [upcomingMissions, setUpcomingMissions] = useState<Mission[]>([])
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMission[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMission>>(new Map<string, EchoMission>())
    const [robotOptions, setRobotOptions] = useState<Map<string, Robot>>(new Map<string, Robot>())
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const echoURL = 'https://echo.equinor.com/mp?instCode='
    const savedAsset = sessionStorage.getItem('assetString')
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

        const assetCode = sessionStorage.getItem('assetString')
        selectedEchoMissions.map((mission: EchoMission) => {
            apiCaller.postMission(mission.id, selectedRobot.id, assetCode)
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        apiCaller.deleteMission(mission.id)
    }

    useEffect(() => {
        apiCaller.getMissionsByStatus(MissionStatus.Pending).then((missions) => {
            setUpcomingMissions(missions)
        })
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            const installationCode = sessionStorage.getItem('assetString')
            if (!installationCode || installationCode === '') {
                setEchoMissions(new Map<string, EchoMission>())
            } else {
                apiCaller.getEchoMissions(installationCode as string).then((missions) => {
                    const mappedEchoMissions: Map<string, EchoMission> = mapEchoMissionToString(missions)
                    setEchoMissions(mappedEchoMissions)
                })
            }
        }, refreshInterval)
        return () => clearInterval(id)
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
                setUpcomingMissions(missions)
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
        if (Array.from(robotOptions.keys()).length === 0 || Array.from(echoMissions.keys()).length === 0) {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [robotOptions, echoMissions])

    var upcomingMissionDisplay = upcomingMissions.map(function (mission, index) {
        return <UpcomingMissionCard key={index} mission={mission} onDeleteMission={onDeleteMission} />
    })
    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {Text('Mission Queue')}
            </Typography>
            <MissionTable>
                {upcomingMissions.length > 0 && upcomingMissionDisplay}
                {upcomingMissions.length === 0 && <NoUpcomingMissionsPlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog
                    robotOptions={Array.from(robotOptions.keys())}
                    echoMissionsOptions={Array.from(echoMissions.keys())}
                    onSelectedMissions={onSelectedEchoMissions}
                    onSelectedRobot={onSelectedRobot}
                    onScheduleButtonPress={onScheduleButtonPress}
                    scheduleButtonDisabled={scheduleButtonDisabled}
                    frontPageScheduleButtonDisabled={frontPageScheduleButtonDisabled}
                ></ScheduleMissionDialog>
                <Button
                    onClick={() => {
                        window.open(echoURL + savedAsset)
                    }}
                >
                    {Text('Create mission')}
                </Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { UpcomingMissionCard } from './UpcomingMissionCard'
import { useApi, useInterval } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { NoUpcomingMissionsPlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { EchoMission } from 'models/EchoMission'

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

export function UpcomingMissionView() {
    const apiCaller = useApi()
    const [upcomingMissions, setUpcomingMissions] = useState<Mission[]>([])
    const [selectedEchoMission, setSelectedEchoMissions] = useState<EchoMission[]>([])
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMission>>()
    const [assetString, setAssetString] = useState<string>('')

    const onSelectedEchoMissions = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: EchoMission[] = []
        selectedEchoMissions.map((selectedEchoMission: string) => {
            if (echoMissions) echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as EchoMission)
        })
        setSelectedEchoMissions(echoMissionsToSchedule)
    }
    const onScheduleButtonPress = () => {
        selectedEchoMission.map((mission: EchoMission) => {
            apiCaller.postMission(mission.id, new Date())
        })
    }
    useEffect(() => {
        apiCaller.getMissionsByStatus(MissionStatus.Pending).then((missions) => {
            setUpcomingMissions(missions)
        })
    }, [])

    useEffect(() => {
        const installationCode = sessionStorage.getItem('assetString')
        if (installationCode != assetString) {
            setAssetString(installationCode as string)
        }
    }, [sessionStorage.getItem('assetString')])

    useEffect(() => {
        apiCaller.getEchoMissions(assetString).then((missions) => {
            const mappedEchoMissions: Map<string, EchoMission> = mapEchoMissionToString(missions)
            setEchoMissions(mappedEchoMissions)
        })
    }, [apiCaller])

    useInterval(async () => {
        apiCaller.getMissionsByStatus(MissionStatus.Pending).then((missions) => {
            setUpcomingMissions(missions)
        })
    })

    var upcomingMissionDisplay = upcomingMissions.map(function (mission, index) {
        return <UpcomingMissionCard key={index} mission={mission} />
    })
    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                {upcomingMissions.length > 0 && upcomingMissionDisplay}
                {upcomingMissions.length === 0 && <NoUpcomingMissionsPlaceholder />}
            </MissionTable>
            <MissionButtonView>
                {echoMissions && (
                    <>
                        <ScheduleMissionDialog
                            options={Array.from(echoMissions.keys())}
                            onSelectedMissions={onSelectedEchoMissions}
                            onScheduleButtonPress={onScheduleButtonPress}
                        ></ScheduleMissionDialog>
                        <Button>Make new mission in Echo</Button>
                    </>
                )}
            </MissionButtonView>
        </StyledMissionView>
    )
}

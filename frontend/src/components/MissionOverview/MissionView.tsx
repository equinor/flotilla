import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { UpcomingMissionCard } from './UpcomingMissionCard'
import { useApi, useInterval } from 'api/ApiCaller'
import { useContext, useEffect, useState } from 'react'
import { Mission } from 'models/Mission'
import { NoUpcomingMissionsPlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Header } from 'components/Header/Header'

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
const processEchoMissions = (missions: Mission[]): string[] => {
    const stringifiedArray: string[] = [];
    missions.map((mission: Mission) => {
        stringifiedArray.push(mission.id + ": " + mission.name);
    })
    return stringifiedArray;
}


export function MissionView() {
    const apiCaller = useApi()
    const [upcomingMissions, setUpcomingMissions] = useState<Mission[]>([])
    const [echoMissions, setEchoMissions] = useState<Mission[]>([]);
    useEffect(() => {
        apiCaller.getUpcomingMissions().then((missions) => {
            setUpcomingMissions(missions)
        })
    }, [])

    useEffect(() => {
        const installationCode = sessionStorage.getItem('assetString')
        apiCaller.getEchoMissionsForPlant(installationCode as string).then((missions) => {
            setEchoMissions(missions)
        })
    }, [apiCaller])

    useInterval(async () => {
        apiCaller.getUpcomingMissions().then((missions) => {
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
                <ScheduleMissionDialog options={processEchoMissions(echoMissions)}></ScheduleMissionDialog>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

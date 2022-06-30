import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionCard } from './MissionCard'
import { useApi, useInterval } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission } from 'models/mission'
import { NoUpcomingMissionsPlaceholder } from './NoMissionPlaceholder'

const refreshTimer = 5000

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

export function MissionView() {
    const apiCaller = useApi()
    const [upcomingMissions, setUpcomingMissions] = useState<Mission[]>([])
    useEffect(() => {
        apiCaller.getUpcomingMissions().then((result) => {
            setUpcomingMissions(result.body)
        })
    }, [])
    useInterval(async () => {
        apiCaller.getUpcomingMissions().then((result) => {
            setUpcomingMissions(result.body)
        })
    }, refreshTimer)

    var missionDisplay = upcomingMissions.map(function (mission, index) {
        return <MissionCard key={index} mission={mission} />
    })

    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                {upcomingMissions.length > 0 && missionDisplay}
                {upcomingMissions.length === 0 && <NoUpcomingMissionsPlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <Button>Schedule mission</Button>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

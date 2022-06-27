import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionCard } from './MissionCard'
import { useApi,  useInterval } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { ScheduledMission } from 'models/scheduledMission'

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
    const [upcomingMissions, setUpcomingMissions] = useState<ScheduledMission[]>([])
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

    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                {upcomingMissions.map(function (scheduledMission, index) {
                    return <MissionCard key={index} scheduledMission={scheduledMission} />
                })}
            </MissionTable>
            <MissionButtonView>
                <Button>Schedule mission</Button>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

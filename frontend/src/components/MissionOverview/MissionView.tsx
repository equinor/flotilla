import { Button, Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionCard } from './MissionCard'
import { useApi } from 'components/SignInPage/ApiCaller'
import { useEffect, useState } from 'react'
import { ScheduledMission } from 'models/scheduledMission'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`
const UpcomingMissionCards = styled.div`
    width: 400px;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    gap: 0.5rem;
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

    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                <UpcomingMissionCards>
                    {upcomingMissions.map(function (scheduledMission, index) {
                        return <MissionCard key={index} scheduledMission={scheduledMission} />
                    })}
                </UpcomingMissionCards>
            </MissionTable>
            <MissionButtonView>
                <Button>Schedule mission</Button>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

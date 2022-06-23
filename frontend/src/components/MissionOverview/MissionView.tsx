import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionCard } from './MissionCard'
import { useApi } from 'components/SignInPage/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission } from 'models/mission'

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

    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                {upcomingMissions.map(function (mission, index) {
                    return <MissionCard key={index} mission={mission} />
                })}
            </MissionTable>
            <MissionButtonView>
                <Button>Schedule mission</Button>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

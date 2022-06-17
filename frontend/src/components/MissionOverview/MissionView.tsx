import { Button, Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionCard } from './MissionCard'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
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
    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                <MissionCard />
                <MissionCard />
                <MissionCard />
            </MissionTable>
            <MissionButtonView>
                <Button>Schedule mission</Button>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}

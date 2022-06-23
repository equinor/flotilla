import { Typography } from '@equinor/eds-core-react'
import { defaultMission } from 'models/mission'
import styled from 'styled-components'
import { OngoingMissionCard } from './OngoingMissionCard'
const testMissions = [defaultMission['test1']]

const StyledOngoingMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    gap: 2rem;
`

export function OngoingMissionView() {
    return (
        <StyledOngoingMissionView>
            <Typography variant="h2" color="resting">
                {' '}
                Ongoing Missions
            </Typography>
            <OngoingMissionSection>
                <OngoingMissionCard mission={testMissions[0]} />
            </OngoingMissionSection>
        </StyledOngoingMissionView>
    )
}

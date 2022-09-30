import { Typography } from '@equinor/eds-core-react'
import { pause } from '@equinor/eds-icons'
import { useApi } from 'api/ApiCaller'
import { Mission, MissionStatus } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'

const StyledOngoingMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`

export function OngoingMissionView() {
    const apiCaller = useApi()
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [pausedMissions, setPausedMissions] = useState<Mission[]>([])
    const [missionsToDisplay, setMissionsToDisplay] = useState<Mission[]>([])
    useEffect(() => {
        apiCaller.getMissionsByStatus(MissionStatus.Ongoing).then((missions) => {
            setOngoingMissions(missions)
        })
        apiCaller.getMissionsByStatus(MissionStatus.Paused).then((pausedMissions) => {
            setPausedMissions(pausedMissions)
        })
    }, [])
    useEffect(() => {
        const missions: Mission[] = ongoingMissions.concat(pausedMissions)
        setMissionsToDisplay(missions)
    }, [ongoingMissions, pausedMissions])

    var missionDisplay = missionsToDisplay.map(function (mission, index) {
        return <OngoingMissionCard key={index} mission={mission} />
    })

    return (
        <StyledOngoingMissionView>
            <Typography variant="h2" color="resting">
                Ongoing missions
            </Typography>
            <OngoingMissionSection>
                {missionsToDisplay.length > 0 && missionDisplay}
                {missionDisplay.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
        </StyledOngoingMissionView>
    )
}

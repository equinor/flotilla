import { Typography } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { Mission, MissionStatus } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
import { Text } from 'components/Contexts/LanguageContext'

const StyledOngoingMissionView = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`

export function OngoingMissionView({ refreshInterval }: RefreshProps) {
    const apiCaller = useApi()
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [pausedMissions, setPausedMissions] = useState<Mission[]>([])
    const [missionsToDisplay, setMissionsToDisplay] = useState<Mission[]>([])

    useEffect(() => {
        updateOngoingMissions()
        updatePausedMissions()
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            updateOngoingMissions()
            updatePausedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    const updateOngoingMissions = () => {
        apiCaller.getMissionsByStatus(MissionStatus.Ongoing).then((missions) => {
            setOngoingMissions(missions)
        })
    }

    const updatePausedMissions = () => {
        apiCaller.getMissionsByStatus(MissionStatus.Paused).then((missions) => {
            setPausedMissions(missions)
        })
    }

    useEffect(() => {
        const missions: Mission[] = ongoingMissions.concat(pausedMissions)
        setMissionsToDisplay(missions)
    }, [ongoingMissions, pausedMissions])

    var missionDisplay = missionsToDisplay.map(function (mission, index) {
        return <OngoingMissionCard key={index} mission={mission} />
    })

    return (
        <StyledOngoingMissionView>
            <Typography variant="h1" color="resting">
                {Text('Ongoing Missions')}
            </Typography>
            <OngoingMissionSection>
                {missionsToDisplay.length > 0 && missionDisplay}
                {missionsToDisplay.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
        </StyledOngoingMissionView>
    )
}

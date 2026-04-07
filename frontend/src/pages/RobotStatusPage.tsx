import { useAlertContext } from 'components/Contexts/AlertContext'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext } from 'react'

import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotCards/RobotStatusCard'

const RobotCardSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`
const RobotView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

export const RobotStatusPage = () => {
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)
    const { enabledRobots } = useAssetContext()

    const relevantRobots = enabledRobots
        .filter(
            (robot) =>
                robot.currentInstallation.installationCode.toLocaleLowerCase() ===
                installation.installationCode.toLocaleLowerCase()
        )
        .sort((robot, robotToCompareWith) =>
            robot.status! !== robotToCompareWith.status!
                ? robot.status! > robotToCompareWith.status!
                    ? 1
                    : -1
                : robot.name! === robotToCompareWith.name!
                  ? 0
                  : robot.name! > robotToCompareWith.name!
                    ? 1
                    : -1
        )

    const robotDisplay = relevantRobots.map((robot) => <RobotStatusCard key={robot.id} robot={robot} />)

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <RobotView>
                <RobotCardSection>
                    {relevantRobots.length > 0 && robotDisplay}
                    {relevantRobots.length === 0 && <RobotStatusCardPlaceholder />}
                </RobotCardSection>
            </RobotView>
        </>
    )
}

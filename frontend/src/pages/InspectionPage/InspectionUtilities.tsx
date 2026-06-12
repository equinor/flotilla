import { Card } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionDefinition } from 'models/MissionDefinition'
import { tokens } from '@equinor/eds-tokens'
import { cardShadow } from 'components/Styles/StyledComponents'

export const StyledCard = styled(Card)`
    display: flex;
    min-height: 150px;
    padding: 16px;
    flex-direction: column;
    justify-content: space-between;
    flex: 1 0 0;
    cursor: pointer;
    border-radius: 2px;
`
export const CardComponent = styled.div`
    display: flex;
    padding-right: 16px;
    justify-content: flex-end;
    gap: 10px;
    width: 100%;
`
export const StyledInspectionAreaCards = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, 450px);
    grid-auto-rows: 1fr;
    gap: 24px;
`
export const InspectionAreaText = styled.div`
    display: grid;
    grid-template-rows: 25px 35px;
    align-self: stretch;
`
export const TopInspectionAreaText = styled.div`
    display: flex;
    justify-content: space-between;
    margin-right: 5px;
`
export const StyledInspectionAreaCard = styled.div`
    display: flex;
    @media (max-width: 800px) {
        max-width: calc(100vw - 30px);
    }
    max-width: 450px;
    border-radius: 2px;
    overflow: hidden;
    box-shadow: ${cardShadow};
`
export const InspectionAreaOverview = styled.div`
    display: flex;
    flex-direction: column;
    gap: 48px;
`
export const Placeholder = styled.div`
    padding: 24px;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    border-radius: 4px;
`
export const Content = styled.div`
    display: flex;
    align-items: centre;
    gap: 5px;
`

/**
 * Compares two mission definitions for sorting. Missions without a last successful
 * run are sorted before missions with one. If neither has a last run, they are
 * sorted alphabetically by name.
 */
export const compareMissionDefinitions = (a: MissionDefinition, b: MissionDefinition) => {
    if (!a.lastSuccessfulRun && !b.lastSuccessfulRun) {
        return a.name > b.name ? 1 : -1
    }
    if (!a.lastSuccessfulRun) return -1
    if (!b.lastSuccessfulRun) return 1
    return a.name > b.name ? 1 : -1
}

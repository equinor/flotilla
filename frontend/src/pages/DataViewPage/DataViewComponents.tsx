import { Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { StyledImagesSection } from 'pages/InspectionReportPage/InspectionStyles'
import styled from 'styled-components'

export const DataViewMapWrapper = styled.div`
    .leaflet-tooltip.circleLabel {
        background-color: ${tokens.colors.ui.background__medium.hex} !important;
        padding: 0 4px !important;
        border-radius: 2px !important;
    }
`
export const WhiteBackgroundBand = styled.div`
    display: flex;
    flex-direction: column;
    align-self: flex-start;
    margin: 0 -3rem;
    padding: 24px 3rem;
    gap: 24px;
    width: 100%;
    background: ${tokens.colors.ui.background__default.hex};
`
export const TimeRangeToggle = styled.div`
    display: inline-flex;
    align-self: flex-start;
    gap: 4px;
    padding: 4px;
    border-radius: 6px;
    box-shadow: inset 0 0 0 1px ${tokens.colors.ui.background__medium.hex};
`
export const TimeRangeToggleButton = styled(Button)`
    border-radius: 4px;
`
export const DataViewChartArea = styled.div`
    display: flex;
    flex-direction: column;
    max-width: 1250px;
    gap: 15px;
`
export const StyledTopAlignedImagesSection = styled(StyledImagesSection)`
    align-items: flex-start;
    gap: 40px;
`
export const StyledDataViewImageCard = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
    max-width: 530px;
`

import { Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { ContentCard } from 'components/Styles/StyledComponents'
import styled from 'styled-components'

export const DataViewMapWrapper = styled.div`
    .leaflet-tooltip.circleLabel {
        background-color: ${tokens.colors.ui.background__medium.hex} !important;
        padding: 0 4px !important;
        border-radius: 2px !important;
    }
`
export const TimeRangeToggle = styled.div`
    display: inline-flex;
    align-self: flex-start;
    flex-wrap: wrap;
    gap: 4px;
    padding: 4px;
    border-radius: 6px;
    box-shadow: inset 0 0 0 1px ${tokens.colors.ui.background__medium.hex};
`
export const TimeRangeToggleButton = styled(Button)`
    border-radius: 4px;
`
export const CustomTimeRangeForm = styled.div`
    display: flex;
    align-items: flex-end;
    flex-wrap: wrap;
    gap: 12px;
    max-width: 600px;
`
export const CustomTimeRangeField = styled.div`
    flex: 1 1 180px;
`
export const CustomTimeRangeError = styled.div`
    color: ${tokens.colors.interactive.danger__text.hex};
    min-height: 20px;
`
export const StyledTopAlignedImagesSection = styled(ContentCard)`
    flex-direction: row;
    align-items: flex-start;
    gap: 40px;
`
export const StyledDataViewImageCard = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
    max-width: 530px;
`

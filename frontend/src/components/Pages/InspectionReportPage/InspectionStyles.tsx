import { Button, Card, Dialog } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { styled } from 'styled-components'
import { phone_width } from 'utils/constants'

export const HiddenOnSmallScreen = styled.div`
    @media (max-width: ${phone_width}) {
        display: none;
    }
`

export const StyledInspection = styled.img<{ $otherContentHeight?: string }>`
    max-height: calc(80vh - ${(props) => props.$otherContentHeight});
    max-width: 100%;
`

export const StyledInspectionImage = styled.img`
    flex: 1 0 0;
    align-self: center;
    max-width: 100%;
`

export const StyledDialog = styled(Dialog)`
    display: flex;
    width: 100%;
    max-height: 80vh;
`
export const StyledCloseButton = styled(Button)`
    width: 24px;
    height: 24px;
`
export const StyledDialogContent = styled(Dialog.Content)`
    display: flex;
    flex-direction: column;
    gap: 10px;
`
export const StyledDialogHeader = styled.div`
    display: flex;
    padding: 16px;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
    border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
    height: 24px;
`

export const StyledBottomContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    padding: 16px;
    gap: 16px;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
`

export const StyledInfoContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`

export const StyledInspectionOverviewSection = styled.div`
    display: flex;
    padding: 24px;
    min-width: 240px;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    border-radius: 6px;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    background: ${tokens.colors.ui.background__default.hex};
`

export const StyledInspectionOverviewDialogView = styled.div`
    display: flex;
    max-height: 60vh;
    width: 350px;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    overflow-y: scroll;
`
export const StyledImagesSection = styled.div`
    display: flex;
    align-items: center;
    gap: 16px;
`

export const StyledImageCard = styled(Card)`
    display: flex;
    width: 150px;
    align-self: stretch;
    padding: 4px;
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
    border-radius: 2px;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    background: ${tokens.colors.ui.background__default.hex};
    box-shadow:
        0px 2.389px 4.778px 0px ${tokens.colors.ui.background__light.hex},
        0px 3.583px 4.778px 0px ${tokens.colors.ui.background__light.hex};
    cursor: pointer;
`

export const StyledInspectionData = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    gap: 8px;
`
export const StyledInspectionContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`
export const StyledDialogInspectionView = styled.div`
    display: flex;
    flex-direction: row;
    gap: 16px;
`

export const StyledInspectionCards = styled.div`
    display: flex;
    justify-content: start;
    align-items: flex-start;
    align-content: flex-start;
    gap: 8px;
    align-self: stretch;
    flex-wrap: wrap;
`

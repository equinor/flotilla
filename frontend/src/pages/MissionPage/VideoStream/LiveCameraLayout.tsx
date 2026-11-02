import styled from 'styled-components'

export const LiveCameraRow = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem 0;
    min-width: 0;
`

export const LiveCamera = styled.div<{ $aspectRatio: number }>`
    flex: ${({ $aspectRatio }) => $aspectRatio} 1 ${({ $aspectRatio }) => $aspectRatio * 12}rem;
    min-width: min(100%, ${({ $aspectRatio }) => $aspectRatio * 12}rem);
    max-width: min(100%, ${({ $aspectRatio }) => $aspectRatio * 20}rem);
`
